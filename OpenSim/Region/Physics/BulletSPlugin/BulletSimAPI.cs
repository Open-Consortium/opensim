/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyrightD
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using OpenMetaverse;

namespace OpenSim.Region.Physics.BulletSPlugin {

// Classes to allow some type checking for the API
// These hold pointers to allocated objects in the unmanaged space.

// The physics engine controller class created at initialization
public struct BulletSim
{
    public BulletSim(uint worldId, BSScene bss, IntPtr xx) { worldID = worldId; scene = bss;  Ptr = xx; }
    public uint worldID;
    // The scene is only in here so very low level routines have a handle to print debug/error messages
    public BSScene scene;
    public IntPtr Ptr;
}

public struct BulletShape
{
    public BulletShape(IntPtr xx) { Ptr = xx; }
    public IntPtr Ptr;
}

// An allocated Bullet btRigidBody
public struct BulletBody
{
    public BulletBody(uint id, IntPtr xx) { ID = id;  Ptr = xx; }
    public IntPtr Ptr;
    public uint ID;
}

// An allocated Bullet btConstraint
public struct BulletConstraint
{
    public BulletConstraint(IntPtr xx) { Ptr = xx; }
    public IntPtr Ptr;
}

// An allocated HeightMapThing which hold various heightmap info
// Made a class rather than a struct so there would be only one
//      instance of this and C# will pass around pointers rather
//      than making copies.
public class BulletHeightMapInfo
{
    public BulletHeightMapInfo(uint id, float[] hm, IntPtr xx) {
        ID = id;
        Ptr = xx;
        heightMap = hm;
        terrainRegionBase = new Vector2(0f, 0f);
        minCoords = new Vector3(100f, 100f, 25f);
        maxCoords = new Vector3(101f, 101f, 26f);
        minZ = maxZ = 0f;
        sizeX = sizeY = 256f;
    }
    public uint ID;
    public IntPtr Ptr;
    public float[] heightMap;
    public Vector2 terrainRegionBase;
    public Vector3 minCoords;
    public Vector3 maxCoords;
    public float sizeX, sizeY;
    public float minZ, maxZ;
    public BulletShape terrainShape;
    public BulletBody terrainBody;
}

// ===============================================================================
[StructLayout(LayoutKind.Sequential)]
public struct ConvexHull 
{
	Vector3 Offset;
	int VertexCount;
	Vector3[] Vertices;
}
[StructLayout(LayoutKind.Sequential)]
public struct ShapeData 
{
    public enum PhysicsShapeType
    {
		SHAPE_UNKNOWN   = 0,
		SHAPE_AVATAR    = 1,
		SHAPE_BOX       = 2,
		SHAPE_CONE      = 3,
		SHAPE_CYLINDER  = 4,
		SHAPE_SPHERE    = 5,
		SHAPE_MESH      = 6,
		SHAPE_HULL      = 7
    };
    public uint ID;
    public PhysicsShapeType Type;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 Scale;
    public float Mass;
    public float Buoyancy;
    public System.UInt64 HullKey;
    public System.UInt64 MeshKey;
    public float Friction;
    public float Restitution;
    public float Collidable;    // true of things bump into this
    public float Static;        // true if a static object. Otherwise gravity, etc.

    // note that bools are passed as floats since bool size changes by language and architecture
    public const float numericTrue = 1f;
    public const float numericFalse = 0f;
}
[StructLayout(LayoutKind.Sequential)]
public struct SweepHit 
{
    public uint ID;
    public float Fraction;
    public Vector3 Normal;
    public Vector3 Point;
}
[StructLayout(LayoutKind.Sequential)]
public struct RaycastHit
{
    public uint ID;
    public float Fraction;
    public Vector3 Normal;
}
[StructLayout(LayoutKind.Sequential)]
public struct CollisionDesc
{
    public uint aID;
    public uint bID;
    public Vector3 point;
    public Vector3 normal;
}
[StructLayout(LayoutKind.Sequential)]
public struct EntityProperties
{
    public uint ID;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 Acceleration;
    public Vector3 RotationalVelocity;
}

// Format of this structure must match the definition in the C++ code
[StructLayout(LayoutKind.Sequential)]
public struct ConfigurationParameters
{
    public float defaultFriction;
    public float defaultDensity;
    public float defaultRestitution;
    public float collisionMargin;
    public float gravity;

    public float linearDamping;
    public float angularDamping;
    public float deactivationTime;
    public float linearSleepingThreshold;
    public float angularSleepingThreshold;
	public float ccdMotionThreshold;
	public float ccdSweptSphereRadius;
    public float contactProcessingThreshold;

    public float terrainFriction;
    public float terrainHitFraction;
    public float terrainRestitution;
    public float avatarFriction;
    public float avatarDensity;
    public float avatarRestitution;
    public float avatarCapsuleRadius;
    public float avatarCapsuleHeight;
	public float avatarContactProcessingThreshold;

	public float maxPersistantManifoldPoolSize;
	public float maxCollisionAlgorithmPoolSize;
	public float shouldDisableContactPoolDynamicAllocation;
	public float shouldForceUpdateAllAabbs;
	public float shouldRandomizeSolverOrder;
	public float shouldSplitSimulationIslands;
	public float shouldEnableFrictionCaching;
	public float numberOfSolverIterations;

    public float linkConstraintUseFrameOffset;
    public float linkConstraintEnableTransMotor;
    public float linkConstraintTransMotorMaxVel;
    public float linkConstraintTransMotorMaxForce;
    public float linkConstraintERP;
    public float linkConstraintCFM;

    public const float numericTrue = 1f;
    public const float numericFalse = 0f;
}


// The states a bullet collision object can have
public enum ActivationState : uint
{
    ACTIVE_TAG = 1,
    ISLAND_SLEEPING,
    WANTS_DEACTIVATION,
    DISABLE_DEACTIVATION,
    DISABLE_SIMULATION
}

// Values used by Bullet and BulletSim to control object properties.
// Bullet's "CollisionFlags" has more to do with operations on the
//    object (if collisions happen, if gravity effects it, ...).
public enum CollisionFlags : uint
{
    CF_STATIC_OBJECT                 = 1 << 0,
    CF_KINEMATIC_OBJECT              = 1 << 1,
    CF_NO_CONTACT_RESPONSE           = 1 << 2,
    CF_CUSTOM_MATERIAL_CALLBACK      = 1 << 3,
    CF_CHARACTER_OBJECT              = 1 << 4,
    CF_DISABLE_VISUALIZE_OBJECT      = 1 << 5,
    CF_DISABLE_SPU_COLLISION_PROCESS = 1 << 6,
    // Following used by BulletSim to control collisions
    BS_SUBSCRIBE_COLLISION_EVENTS    = 1 << 10,
    BS_VOLUME_DETECT_OBJECT          = 1 << 11,
    BS_PHANTOM_OBJECT                = 1 << 12,
    BS_PHYSICAL_OBJECT               = 1 << 13,
    BS_TERRAIN_OBJECT                = 1 << 14,
    BS_NONE                          = 0,
    BS_ALL                           = 0xFFFFFFFF
};

// Values for collisions groups and masks
public enum CollisionFilterGroups : uint
{
    NoneFilter              = 0,
    DefaultFilter           = 1 << 0,
    StaticFilter            = 1 << 1,
    KinematicFilter         = 1 << 2,
    DebrisFilter            = 1 << 3,
    SensorTrigger           = 1 << 4,
    CharacterFilter         = 1 << 5,
    AllFilter               = 0xFFFFFFFF,
    // Filter groups defined by BulletSim
    GroundPlaneFilter       = 1 << 10,
    TerrainFilter           = 1 << 11,
    RaycastFilter           = 1 << 12,
    SolidFilter             = 1 << 13,
};

    // For each type, we first clear and then set the collision flags
public enum ClearCollisionFlag : uint
{
    Terrain = CollisionFlags.BS_ALL,
    Phantom = CollisionFlags.BS_ALL,
    VolumeDetect = CollisionFlags.BS_ALL,
    PhysicalObject = CollisionFlags.BS_ALL,
    StaticObject = CollisionFlags.BS_ALL
}

public enum SetCollisionFlag : uint
{
    Terrain = CollisionFlags.CF_STATIC_OBJECT
        | CollisionFlags.BS_TERRAIN_OBJECT,
    Phantom = CollisionFlags.CF_STATIC_OBJECT
        | CollisionFlags.BS_PHANTOM_OBJECT
        | CollisionFlags.CF_NO_CONTACT_RESPONSE,
    VolumeDetect = CollisionFlags.CF_STATIC_OBJECT
        | CollisionFlags.BS_VOLUME_DETECT_OBJECT
        | CollisionFlags.CF_NO_CONTACT_RESPONSE,
    PhysicalObject = CollisionFlags.BS_PHYSICAL_OBJECT,
    StaticObject = CollisionFlags.CF_STATIC_OBJECT,
}

// Collision filters used for different types of objects
public enum SetCollisionFilter : uint
{
    Terrain = CollisionFilterGroups.AllFilter,
    Phantom = CollisionFilterGroups.GroundPlaneFilter
        | CollisionFilterGroups.TerrainFilter,
    VolumeDetect = CollisionFilterGroups.AllFilter,
    PhysicalObject = CollisionFilterGroups.AllFilter,
    StaticObject = CollisionFilterGroups.AllFilter,
}

// Collision masks used for different types of objects
public enum SetCollisionMask : uint
{
    Terrain = CollisionFilterGroups.AllFilter,
    Phantom = CollisionFilterGroups.GroundPlaneFilter
        | CollisionFilterGroups.TerrainFilter,
    VolumeDetect = CollisionFilterGroups.AllFilter,
    PhysicalObject = CollisionFilterGroups.AllFilter,
    StaticObject = CollisionFilterGroups.AllFilter
}

// CFM controls the 'hardness' of the constraint. 0=fixed, 0..1=violatable. Default=0
// ERP controls amount of correction per tick. Usable range=0.1..0.8. Default=0.2.
public enum ConstraintParams : int
{
    BT_CONSTRAINT_ERP = 1,  // this one is not used in Bullet as of 20120730
    BT_CONSTRAINT_STOP_ERP,
    BT_CONSTRAINT_CFM,
    BT_CONSTRAINT_STOP_CFM,
};
public enum ConstraintParamAxis : int
{
    AXIS_LINEAR_X = 0,
    AXIS_LINEAR_Y,
    AXIS_LINEAR_Z,
    AXIS_ANGULAR_X,
    AXIS_ANGULAR_Y,
    AXIS_ANGULAR_Z,
    AXIS_LINEAR_ALL = 20,    // these last three added by BulletSim so we don't have to do zillions of calls
    AXIS_ANGULAR_ALL,
    AXIS_ALL
};

// ===============================================================================
static class BulletSimAPI {

// Link back to the managed code for outputting log messages
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void DebugLogCallback([MarshalAs(UnmanagedType.LPStr)]string msg);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
[return: MarshalAs(UnmanagedType.LPStr)]
public static extern string GetVersion();

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern uint Initialize(Vector3 maxPosition, IntPtr parms, 
                        int maxCollisions, IntPtr collisionArray, 
                        int maxUpdates, IntPtr updateArray,
                        DebugLogCallback logRoutine);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void CreateInitialGroundPlaneAndTerrain(uint worldID);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetHeightmap(uint worldID, [MarshalAs(UnmanagedType.LPArray)] float[] heightMap);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void Shutdown(uint worldID);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool UpdateParameter(uint worldID, uint localID,
                        [MarshalAs(UnmanagedType.LPStr)]string paramCode, float value);

// ===============================================================================
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern int PhysicsStep(uint worldID, float timeStep, int maxSubSteps, float fixedTimeStep, 
                        out int updatedEntityCount, 
                        out IntPtr updatedEntitiesPtr,
                        out int collidersCount,
                        out IntPtr collidersPtr);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool CreateHull(uint worldID, System.UInt64 meshKey, 
                            int hullCount, [MarshalAs(UnmanagedType.LPArray)] float[] hulls
    );

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool CreateMesh(uint worldID, System.UInt64 meshKey, 
                            int indexCount, [MarshalAs(UnmanagedType.LPArray)] int[] indices,
                            int verticesCount, [MarshalAs(UnmanagedType.LPArray)] float[] vertices
    );

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool DestroyHull(uint worldID, System.UInt64 meshKey);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool DestroyMesh(uint worldID, System.UInt64 meshKey);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool CreateObject(uint worldID, ShapeData shapeData);

/*  Remove old functionality
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void CreateLinkset(uint worldID, int objectCount, ShapeData[] shapeDatas);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void AddConstraint(uint worldID, uint id1, uint id2, 
                        Vector3 frame1, Quaternion frame1rot,
                        Vector3 frame2, Quaternion frame2rot,
                        Vector3 lowLinear, Vector3 hiLinear, Vector3 lowAngular, Vector3 hiAngular);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool RemoveConstraintByID(uint worldID, uint id1);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool RemoveConstraint(uint worldID, uint id1, uint id2);
 */

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetObjectPosition(uint WorldID, uint id);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Quaternion GetObjectOrientation(uint WorldID, uint id);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectTranslation(uint worldID, uint id, Vector3 position, Quaternion rotation);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectVelocity(uint worldID, uint id, Vector3 velocity);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectAngularVelocity(uint worldID, uint id, Vector3 angularVelocity);

// Set the current force acting on the object
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectForce(uint worldID, uint id, Vector3 force);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectScaleMass(uint worldID, uint id, Vector3 scale, float mass, bool isDynamic);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectCollidable(uint worldID, uint id, bool phantom);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectDynamic(uint worldID, uint id, bool isDynamic, float mass);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectGhost(uint worldID, uint id, bool ghostly);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectProperties(uint worldID, uint id, bool isStatic, bool isSolid, bool genCollisions, float mass);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetObjectBuoyancy(uint worldID, uint id, float buoyancy);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool HasObject(uint worldID, uint id);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool DestroyObject(uint worldID, uint id);

// ===============================================================================
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern SweepHit ConvexSweepTest(uint worldID, uint id, Vector3 to, float extraMargin);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern RaycastHit RayTest(uint worldID, uint id, Vector3 from, Vector3 to);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 RecoverFromPenetration(uint worldID, uint id);

// ===============================================================================
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void DumpBulletStatistics();

// Log a debug message
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetDebugLogCallback(DebugLogCallback callback);

// ===============================================================================
// ===============================================================================
// ===============================================================================
// A new version of the API that enables moving all the logic out of the C++ code and into
//    the C# code. This will make modifications easier for the next person.
// This interface passes the actual pointers to the objects in the unmanaged
//    address space. All the management (calls for creation/destruction/lookup)
//    is done in the C# code.
// The names have a "2" tacked on. This will be removed as the C# code gets rebuilt
//    and the old code is removed.

// Functions use while converting from API1 to API2. Can be removed when totally converted.
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr GetSimHandle2(uint worldID);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr GetBodyHandleWorldID2(uint worldID, uint id);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr GetBodyHandle2(IntPtr world, uint id);

// ===============================================================================
// Initialization and simulation
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr Initialize2(Vector3 maxPosition, IntPtr parms,
											int maxCollisions,  IntPtr collisionArray,
											int maxUpdates, IntPtr updateArray);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool UpdateParameter2(IntPtr world, uint localID, String parm, float value);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetHeightMap2(IntPtr world, float[] heightmap);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void Shutdown2(IntPtr sim);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern int PhysicsStep2(IntPtr world, float timeStep, int maxSubSteps, float fixedTimeStep,
                        out int updatedEntityCount, 
                        out IntPtr updatedEntitiesPtr,
                        out int collidersCount,
                        out IntPtr collidersPtr);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool PushUpdate2(IntPtr obj);

// =====================================================================================
// Mesh, hull, shape and body creation helper routines
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateMeshShape2(IntPtr world, 
                int indicesCount, [MarshalAs(UnmanagedType.LPArray)] int[] indices, 
                int verticesCount, [MarshalAs(UnmanagedType.LPArray)] float[] vertices );

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateHullShape2(IntPtr world,
                int hullCount, [MarshalAs(UnmanagedType.LPArray)] float[] hulls);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr BuildHullShape2(IntPtr world, IntPtr meshShape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr BuildNativeShape2(IntPtr world,
                float shapeType, float collisionMargin, Vector3 scale);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool DeleteCollisionShape2(IntPtr world, IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateBodyFromShape2(IntPtr sim, IntPtr shape, Vector3 pos, Quaternion rot);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateBodyFromShapeAndInfo2(IntPtr sim, IntPtr shape, IntPtr constructionInfo);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateBodyWithDefaultMotionState2(IntPtr shape, Vector3 pos, Quaternion rot);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr AllocateBodyInfo2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ReleaseBodyInfo2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void DestroyObject2(IntPtr sim, IntPtr obj);

// =====================================================================================
// Terrain creation and helper routines
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void DumpMapInfo(IntPtr sim, IntPtr manInfo);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateHeightMapInfo2(IntPtr sim, uint id, Vector3 minCoords, Vector3 maxCoords, 
        [MarshalAs(UnmanagedType.LPArray)] float[] heightMap, float collisionMargin);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr FillHeightMapInfo2(IntPtr sim, IntPtr mapInfo, uint id, Vector3 minCoords, Vector3 maxCoords, 
        [MarshalAs(UnmanagedType.LPArray)] float[] heightMap, float collisionMargin);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool ReleaseHeightMapInfo2(IntPtr heightMapInfo);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateGroundPlaneShape2(uint id, float height, float collisionMargin);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateTerrainShape2(IntPtr mapInfo);

// =====================================================================================
// Constraint creation and helper routines
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr Create6DofConstraint2(IntPtr world, IntPtr obj1, IntPtr obj2,
                    Vector3 frame1loc, Quaternion frame1rot,
                    Vector3 frame2loc, Quaternion frame2rot,
                    bool useLinearReferenceFrameA, bool disableCollisionsBetweenLinkedBodies);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr Create6DofConstraintToPoint2(IntPtr world, IntPtr obj1, IntPtr obj2,
                    Vector3 joinPoint,
                    bool useLinearReferenceFrameA, bool disableCollisionsBetweenLinkedBodies);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr CreateHingeConstraint2(IntPtr world, IntPtr obj1, IntPtr obj2,
                    Vector3 pivotinA, Vector3 pivotinB,
                    Vector3 axisInA, Vector3 axisInB,
                    bool useLinearReferenceFrameA, bool disableCollisionsBetweenLinkedBodies);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetConstraintEnable2(IntPtr constrain, float numericTrueFalse);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetConstraintNumSolverIterations2(IntPtr constrain, float iterations);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetFrames2(IntPtr constrain, 
                Vector3 frameA, Quaternion frameArot, Vector3 frameB, Quaternion frameBrot);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetLinearLimits2(IntPtr constrain, Vector3 low, Vector3 hi);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetAngularLimits2(IntPtr constrain, Vector3 low, Vector3 hi);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool UseFrameOffset2(IntPtr constrain, float enable);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool TranslationalLimitMotor2(IntPtr constrain, float enable, float targetVel, float maxMotorForce);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetBreakingImpulseThreshold2(IntPtr constrain, float threshold);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool CalculateTransforms2(IntPtr constrain);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool SetConstraintParam2(IntPtr constrain, ConstraintParams paramIndex, float value, ConstraintParamAxis axis);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool DestroyConstraint2(IntPtr world, IntPtr constrain);

// =====================================================================================
// btCollisionWorld entries
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void UpdateSingleAabb2(IntPtr world, IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void UpdateAabbs2(IntPtr world);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool GetForceUpdateAllAabbs2(IntPtr world);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetForceUpdateAllAabbs2(IntPtr world, bool force);

// =====================================================================================
// btDynamicsWorld entries
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool AddObjectToWorld2(IntPtr world, IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool RemoveObjectFromWorld2(IntPtr world, IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool AddConstraintToWorld2(IntPtr world, IntPtr constrain, bool disableCollisionsBetweenLinkedObjects);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool RemoveConstraintFromWorld2(IntPtr world, IntPtr constrain);
// =====================================================================================
// btCollisionObject entries
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetAnisotripicFriction2(IntPtr constrain);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 SetAnisotripicFriction2(IntPtr constrain, Vector3 frict);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool HasAnisotripicFriction2(IntPtr constrain);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetContactProcessingThreshold2(IntPtr obj, float val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetContactProcessingThreshold2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsStaticObject2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsKinematicObject2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsStaticOrKinematicObject2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool HasContactResponse2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetCollisionShape2(IntPtr sim, IntPtr obj, IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr GetCollisionShape2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern int GetActivationState2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetActivationState2(IntPtr obj, int state);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetDeactivationTime2(IntPtr obj, float dtime);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetDeactivationTime2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ForceActivationState2(IntPtr obj, ActivationState state);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void Activate2(IntPtr obj, bool forceActivation);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsActive2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetRestitution2(IntPtr obj, float val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetRestitution2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetFriction2(IntPtr obj, float val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetFriction2(IntPtr obj);

    /* Haven't defined the type 'Transform'
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Transform GetWorldTransform2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void setWorldTransform2(IntPtr obj, Transform trans);
     */

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetPosition2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Quaternion GetOrientation2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetTranslation2(IntPtr obj, Vector3 position, Quaternion rotation);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr GetBroadphaseHandle2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetBroadphaseHandle2(IntPtr obj, IntPtr handle);

    /*
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Transform GetInterpolationWorldTransform2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetInterpolationWorldTransform2(IntPtr obj, Transform trans);
     */

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetInterpolationLinearVelocity2(IntPtr obj, Vector3 vel);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetInterpolationAngularVelocity2(IntPtr obj, Vector3 vel);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetInterpolationVelocity2(IntPtr obj, Vector3 linearVel, Vector3 angularVel);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetHitFraction2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetHitFraction2(IntPtr obj, float val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern CollisionFlags GetCollisionFlags2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern CollisionFlags SetCollisionFlags2(IntPtr obj, CollisionFlags flags);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern CollisionFlags AddToCollisionFlags2(IntPtr obj, CollisionFlags flags);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern CollisionFlags RemoveFromCollisionFlags2(IntPtr obj, CollisionFlags flags);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetCcdMotionThreshold2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetCcdMotionThreshold2(IntPtr obj, float val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetCcdSweepSphereRadius2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetCcdSweepSphereRadius2(IntPtr obj, float val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr GetUserPointer2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetUserPointer2(IntPtr obj, IntPtr val);

// =====================================================================================
// btRigidBody entries
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ApplyGravity2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetGravity2(IntPtr obj, Vector3 val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetGravity2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetDamping2(IntPtr obj, float lin_damping, float ang_damping);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetLinearDamping2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetAngularDamping2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetLinearSleepingThreshold2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetAngularSleepingThreshold2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ApplyDamping2(IntPtr obj, float timeStep);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetMassProps2(IntPtr obj, float mass, Vector3 inertia);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetLinearFactor2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetLinearFactor2(IntPtr obj, Vector3 factor);

    /*
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetCenterOfMassTransform2(IntPtr obj, Transform trans);
     */

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetCenterOfMassByPosRot2(IntPtr obj, Vector3 pos, Quaternion rot);

// Add a force to the object as if its mass is one.
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ApplyCentralForce2(IntPtr obj, Vector3 force);

// Set the force being applied to the object as if its mass is one.
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetObjectForce2(IntPtr obj, Vector3 force);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetTotalForce2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetTotalTorque2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetInvInertiaDiagLocal2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetInvInertiaDiagLocal2(IntPtr obj, Vector3 inert);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetSleepingThresholds2(IntPtr obj, float lin_threshold, float ang_threshold);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ApplyTorque2(IntPtr obj, Vector3 torque);

// Apply force at the given point. Will add torque to the object.
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ApplyForce2(IntPtr obj, Vector3 force, Vector3 pos);

// Apply impulse to the object. Same as "ApplycentralForce" but force scaled by object's mass.
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ApplyCentralImpulse2(IntPtr obj, Vector3 imp);

// Apply impulse to the object's torque. Force is scaled by object's mass.
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ApplyTorqueImpulse2(IntPtr obj, Vector3 imp);

// Apply impulse at the point given. For is scaled by object's mass and effects both linear and angular forces.
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ApplyImpulse2(IntPtr obj, Vector3 imp, Vector3 pos);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ClearForces2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void ClearAllForces2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void UpdateInertiaTensor2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetCenterOfMassPosition2(IntPtr obj);

    /*
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Transform GetCenterOfMassTransform2(IntPtr obj);
     */

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetLinearVelocity2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetAngularVelocity2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetLinearVelocity2(IntPtr obj, Vector3 val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetAngularVelocity2(IntPtr obj, Vector3 angularVelocity);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetVelocityInLocalPoint2(IntPtr obj, Vector3 pos);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void Translate2(IntPtr obj, Vector3 trans);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void UpdateDeactivation2(IntPtr obj, float timeStep);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool WantsSleeping2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetAngularFactor2(IntPtr obj, float factor);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetAngularFactorV2(IntPtr obj, Vector3 factor);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetAngularFactor2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsInWorld2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void AddConstraintRef2(IntPtr obj, IntPtr constrain);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void RemoveConstraintRef2(IntPtr obj, IntPtr constrain);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern IntPtr GetConstraintRef2(IntPtr obj, int index);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern int GetNumConstraintRefs2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetDeltaLinearVelocity2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetDeltaAngularVelocity2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetPushVelocity2(IntPtr obj);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetTurnVelocity2(IntPtr obj);

// =====================================================================================
// btCollisionShape entries

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetAngularMotionDisc2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetContactBreakingThreshold2(IntPtr shape, float defaultFactor);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsPolyhedral2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsConvex2d2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsConvex2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsNonMoving2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsConcave2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsCompound2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsSoftBody2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern bool IsInfinite2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetLocalScaling2(IntPtr shape, Vector3 scale);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern Vector3 GetLocalScaling2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void CalculateLocalInertia2(IntPtr shape, float mass, Vector3 inertia);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern int GetShapeType2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetMargin2(IntPtr shape, float val);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern float GetMargin2(IntPtr shape);

[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void SetCollisionFilterMask(IntPtr shape, uint filter, uint mask);

// =====================================================================================
// Debugging
[DllImport("BulletSim", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
public static extern void DumpPhysicsStatistics2(IntPtr sim);

}
}
