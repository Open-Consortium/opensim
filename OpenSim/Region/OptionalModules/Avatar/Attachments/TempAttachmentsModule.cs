/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Monitoring;
using OpenSim.Region.ClientStack.LindenUDP;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.OptionalModules.Avatar.Attachments
{
    /// <summary>
    /// A module that just holds commands for inspecting avatar appearance.
    /// </summary>
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "TempAttachmentsModule")]
    public class TempAttachmentsModule : INonSharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;

        public void Initialise(IConfigSource configSource)
        {
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            m_scene = scene;

            IScriptModuleComms comms = scene.RequestModuleInterface<IScriptModuleComms>();
            if (comms != null)
            {
                comms.RegisterScriptInvocation( this, "llAttachToAvatarTemp");
                m_log.DebugFormat("[TEMP ATTACHS]: Registered script functions");
            }
            else
            {
                m_log.ErrorFormat("[TEMP ATTACHS]: Failed to register script functions");
            }
        }

        public void Close()
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "TempAttachmentsModule"; }
        }

        private void llAttachToAvatarTemp(UUID host, UUID script, int attachmentPoint)
        {
            SceneObjectPart hostPart = m_scene.GetSceneObjectPart(host);

            if (hostPart == null)
                return;

            if (hostPart.ParentGroup.IsAttachment)
                return;

            IAttachmentsModule attachmentsModule = m_scene.RequestModuleInterface<IAttachmentsModule>();
            if (attachmentsModule == null)
                return;

            TaskInventoryItem item = hostPart.Inventory.GetInventoryItem(script);
            if (item == null)
                return;

            if ((item.PermsMask & 32) == 0) // PERMISSION_ATTACH
                return;

            ScenePresence target;
            if (!m_scene.TryGetScenePresence(item.PermsGranter, out target))
                return;
            
            if (target.UUID != hostPart.ParentGroup.OwnerID)
            {
                uint effectivePerms = hostPart.ParentGroup.GetEffectivePermissions();

                if ((effectivePerms & (uint)PermissionMask.Transfer) == 0)
                    return;

                hostPart.ParentGroup.SetOwnerId(target.UUID);
                hostPart.ParentGroup.SetRootPartOwner(hostPart.ParentGroup.RootPart, target.UUID, target.ControllingClient.ActiveGroupId);

                if (m_scene.Permissions.PropagatePermissions())
                {
                    foreach (SceneObjectPart child in hostPart.ParentGroup.Parts)
                    {
                        child.Inventory.ChangeInventoryOwner(target.UUID);
                        child.TriggerScriptChangedEvent(Changed.OWNER);
                        child.ApplyNextOwnerPermissions();
                    }
                }

                hostPart.ParentGroup.RootPart.ObjectSaleType = 0;
                hostPart.ParentGroup.RootPart.SalePrice = 10;

                hostPart.ParentGroup.HasGroupChanged = true;
                hostPart.ParentGroup.RootPart.SendPropertiesToClient(target.ControllingClient);
                hostPart.ParentGroup.RootPart.ScheduleFullUpdate();
            }

            attachmentsModule.AttachObject(target, hostPart.ParentGroup, (uint)attachmentPoint, false, true, true);
        }
    }
}
