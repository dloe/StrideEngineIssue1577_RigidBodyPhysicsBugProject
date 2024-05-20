using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace RigidBodyPhysicBug
{

    public class BasicGunBallScript : AsyncScript
    {
        [DataMember(0)]
        public CameraComponent Camera = null;

        [DataMember(1)]
        public Material BallMaterial = null;
        

        /*
        [DataMember(2)]
        public Model BulletModel;

        [DataMember(3)]
        public PhysicsComponent physicsComponent;
        */

     

        private List<Material> materialList = new List<Material>(); // Used when BallMaterial is null
        public override async Task Execute()
        {
            var random = new Random(Environment.TickCount);

            // Camera
            if (Camera == null)
            {
                Camera = SceneSystem.SceneInstance.RootScene.Entities.First(e => e.Components.Any(c => c is CameraComponent)).Get<CameraComponent>();
            }
            // Balls Material
            if (BallMaterial == null)
            {
                materialList.Add(CreateMaterialWithTexture(CreateCheckeredPatternTexture(512, 512, Color.White, Color.Red)));
                materialList.Add(CreateMaterialWithTexture(CreateCheckeredPatternTexture(512, 512, Color.White, Color.Yellow)));
                materialList.Add(CreateMaterialWithTexture(CreateCheckeredPatternTexture(512, 512, Color.White, Color.Green)));
                materialList.Add(CreateMaterialWithTexture(CreateCheckeredPatternTexture(512, 512, Color.White, Color.Cyan)));
                materialList.Add(CreateMaterialWithTexture(CreateCheckeredPatternTexture(512, 512, Color.White, Color.Blue)));
                materialList.Add(CreateMaterialWithTexture(CreateCheckeredPatternTexture(512, 512, Color.White, Color.Purple)));

                Trace.WriteLine("BasicGunScript : Material Créer");
            }

            while (Game.IsRunning)
            {
                await Script.NextFrame();

                // Updated ball injection int with space key
                if (Input.IsKeyPressed(Keys.Space))
                {
                    if (BallMaterial is null)
                    {
                        EmitBall(Camera, materialList[random.Next(0, materialList.Count)]);
                    }
                    else
                    {
                        EmitBall(Camera, BallMaterial);
                    }
                }

            }

        }

        private Material CreateMaterialWithTexture(Texture texture)
        {
            var descriptor = new MaterialDescriptor
            {
                Attributes = new MaterialAttributes
                {
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.1f))
                    {
                        Invert = false,
                    },
                    Diffuse = new MaterialDiffuseMapFeature
                    {
                        DiffuseMap = new ComputeTextureColor
                        {
                            Texture = texture,
                            FallbackValue = new ComputeColor(Color4.White),
                            TexcoordIndex = TextureCoordinate.Texcoord0,
                            AddressModeU = TextureAddressMode.Wrap,
                            AddressModeV = TextureAddressMode.Wrap,
                            Filtering = TextureFilter.Linear,
                            Offset = Vector2.Zero,
                            Scale = Vector2.One,
                        }
                    },
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    CullMode = CullMode.Back,
                    Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0f)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature
                    {
                        Fresnel = new MaterialSpecularMicrofacetFresnelSchlick(),
                        Visibility = new MaterialSpecularMicrofacetVisibilitySmithSchlickGGX(),
                        NormalDistribution = new MaterialSpecularMicrofacetNormalDistributionGGX(),
                    },
                },
            };
            return Material.New(GraphicsDevice, descriptor);
        }
        private Texture CreateCheckeredPatternTexture(int width, int height, Color first, Color second, int squareWidth = 0, int squareHeight = 0)
        {
            squareWidth = squareWidth <= 0 ? width / 2 : squareWidth;
            squareHeight = squareHeight <= 0 ? height / 2 : squareHeight;

            Color[] pixels = new Color[width * height];
            void SetPixel(int x, int y, Color value) => pixels[y * width + x] = value;

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if ((((x / squareWidth) & 1) + (y / squareHeight & 1)) == 1)
                    {
                        SetPixel(x, y, second);
                    }
                    else
                    {
                        SetPixel(x, y, first);
                    }
                }
            }

            return Texture.New2D(GraphicsDevice, width, height, PixelFormat.R8G8B8A8_UNorm, pixels);
        }




        public class LifeTimeOfBall : SyncScript
        {
            public override void Update()
            {
                if (Entity.Transform.Position.Y < -50f)
                {
                    Entity.Scene.Entities.Remove(Entity);
                    Entity.Dispose();
                }
            }
        }
        private void EmitBall(CameraComponent camera, Material material)
        {
            var direction = Vector3.Transform(-Vector3.UnitZ, camera.Entity.Transform.Rotation);

            var entity = new Entity("Bullet", camera.Entity.Transform.Position);

            entity.GetOrCreate<ModelComponent>().Model = new Model
            {
                new Mesh { Draw = GeometricPrimitive.Sphere.New(GraphicsDevice,0.01f ).ToMeshDraw() },
                material,
            };

            var rigidbody = entity.GetOrCreate<RigidbodyComponent>();
            
            rigidbody.ColliderShapes.Add(new SphereColliderShapeDesc { Is2D = false, LocalOffset = Vector3.Zero, Radius = 0.01f });
            
            /*
            var buo = new LiguidBuoyancy();
            buo.By_PassMode = true;
            buo.UnityMode = true;
            entity.Add(buo);
            */

            entity.Add(new LifeTimeOfBall());

            Entity.Scene.Entities.Add(entity);

            rigidbody.ApplyImpulse(direction * 1f);
        }

        /*
        public class HeightfieldColliderShapeWithoutDebugPrimitiveCrash : HeightfieldColliderShape
        {
            public HeightfieldColliderShapeWithoutDebugPrimitiveCrash(int heightStickWidth, int heightStickLength, UnmanagedArray<float> dynamicFieldData, float heightScale, float minHeight, float maxHeight, bool flipQuadEdges)
                : base(heightStickWidth, heightStickLength, dynamicFieldData, heightScale, minHeight, maxHeight, flipQuadEdges)
            {
                // DebugPhysicsShapesでデバッグプリミティブを表示しようとしたときにクラッシュするのを回避する
                // ColliderShapeTypesに無い値を指定する(現状0-7がある)
                Type = (ColliderShapeTypes)Enum.ToObject(typeof(ColliderShapeTypes), 10);
            }
        }
        */
    }


}