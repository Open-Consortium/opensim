////////////////////////////////////////////////////////////////
//
// (c) 2009, 2010 Careminster Limited and Melanie Thielker
//
// All rights reserved
//
using System;
using Nini.Config;
using OpenSim.Framework;
using OpenMetaverse;

namespace OpenSim.Services.Interfaces
{
    public interface IBakedTextureModule
    {
        WearableCacheItem[] Get(UUID id);
        void Store(UUID id);
        void UpdateMeshAvatar(UUID id);
    }
}
