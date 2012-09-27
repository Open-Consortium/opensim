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

using System.Collections.Generic;
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;

using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

namespace OpenSim.Region.CoreModules.Agent.AssetTransaction
{
    /// <summary>
    /// Manage asset transactions for a single agent.
    /// </summary>
    public class AgentAssetTransactions
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Fields
        private bool m_dumpAssetsToFile;
        private Scene m_Scene;
        private Dictionary<UUID, AssetXferUploader> XferUploaders = new Dictionary<UUID, AssetXferUploader>();

        // Methods
        public AgentAssetTransactions(UUID agentID, Scene scene,
                bool dumpAssetsToFile)
        {
            m_Scene = scene;
            m_dumpAssetsToFile = dumpAssetsToFile;
        }

        /// <summary>
        /// Return the xfer uploader for the given transaction.
        /// </summary>
        /// <remarks>
        /// If an uploader does not already exist for this transaction then it is created, otherwise the existing
        /// uploader is returned.
        /// </remarks>
        /// <param name="transactionID"></param>
        /// <returns>The asset xfer uploader</returns>
        public AssetXferUploader RequestXferUploader(UUID transactionID)
        {
            AssetXferUploader uploader;

            lock (XferUploaders)
            {
                if (!XferUploaders.ContainsKey(transactionID))
                {
                    uploader = new AssetXferUploader(this, m_Scene, m_dumpAssetsToFile);

//                    m_log.DebugFormat(
//                        "[AGENT ASSETS TRANSACTIONS]: Adding asset xfer uploader {0} since it didn't previously exist", transactionID);

                    XferUploaders.Add(transactionID, uploader);
                }
                else
                {
                    uploader = XferUploaders[transactionID];
                }
            }

            return uploader;
        }

        public void HandleXfer(ulong xferID, uint packetID, byte[] data)
        {
            AssetXferUploader foundUploader = null;

            lock (XferUploaders)
            {
                foreach (AssetXferUploader uploader in XferUploaders.Values)
                {
//                    m_log.DebugFormat(
//                        "[AGENT ASSETS TRANSACTIONS]: In HandleXfer, inspect xfer upload with xfer id {0}",
//                        uploader.XferID);

                    if (uploader.XferID == xferID)
                    {
                        foundUploader = uploader;
                        break;
                    }
                }
            }

            if (foundUploader != null)
            {
//                m_log.DebugFormat(
//                    "[AGENT ASSETS TRANSACTIONS]: Found xfer uploader for xfer id {0}, packet id {1}, data length {2}",
//                    xferID, packetID, data.Length);

                foundUploader.HandleXferPacket(xferID, packetID, data);
            }
            else
            {
                m_log.ErrorFormat(
                    "[AGENT ASSET TRANSACTIONS]: Could not find uploader for xfer id {0}, packet id {1}, data length {2}",
                    xferID, packetID, data.Length);
            }
        }

        public bool RemoveXferUploader(UUID transactionID)
        {
            lock (XferUploaders)
            {
                bool removed = XferUploaders.Remove(transactionID);

                if (!removed)
                    m_log.WarnFormat(
                        "[AGENT ASSET TRANSACTIONS]: Received request to remove xfer uploader with transaction ID {0} but none found",
                        transactionID);
//                else
//                    m_log.DebugFormat(
//                        "[AGENT ASSET TRANSACTIONS]: Removed xfer uploader with transaction ID {0}", transactionID);

                return removed;
            }
        }

        public void RequestCreateInventoryItem(IClientAPI remoteClient,
                UUID transactionID, UUID folderID, uint callbackID,
                string description, string name, sbyte invType,
               sbyte type, byte wearableType, uint nextOwnerMask)
        {
            AssetXferUploader uploader = RequestXferUploader(transactionID);

            uploader.RequestCreateInventoryItem(
                remoteClient, transactionID, folderID, callbackID,
                description, name, invType, type, wearableType, nextOwnerMask);
        }

        /// <summary>
        /// Get an uploaded asset. If the data is successfully retrieved,
        /// the transaction will be removed.
        /// </summary>
        /// <param name="transactionID"></param>
        /// <returns>The asset if the upload has completed, null if it has not.</returns>
        private AssetBase GetTransactionAsset(UUID transactionID)
        {
            lock (XferUploaders)
            {
                if (XferUploaders.ContainsKey(transactionID))
                {
                    AssetXferUploader uploader = XferUploaders[transactionID];
                    AssetBase asset = uploader.GetAssetData();
                    RemoveXferUploader(transactionID);

                    return asset;
                }
            }

            return null;
        }

        public void RequestUpdateTaskInventoryItem(IClientAPI remoteClient,
                SceneObjectPart part, UUID transactionID,
                TaskInventoryItem item)
        {
            AssetBase asset = GetTransactionAsset(transactionID);

            // Only legacy viewers use this, and they prefer CAPS, which
            // we have, so this really never runs.
            // Allow it, but only for "safe" types.
            if ((InventoryType)item.InvType != InventoryType.Notecard &&
                (InventoryType)item.InvType != InventoryType.LSL)
                return;

            if (asset != null)
            {
//                    m_log.DebugFormat(
//                        "[AGENT ASSETS TRANSACTIONS]: Updating item {0} in {1} for transaction {2}", 
//                        item.Name, part.Name, transactionID);
                
                asset.FullID = UUID.Random();
                asset.Name = item.Name;
                asset.Description = item.Description;
                asset.Type = (sbyte)item.Type;
                item.AssetID = asset.FullID;

                m_Scene.AssetService.Store(asset);
            }
        }

        public void RequestUpdateInventoryItem(IClientAPI remoteClient,
                UUID transactionID, InventoryItemBase item)
        {
            AssetXferUploader uploader = RequestXferUploader(transactionID);

            uploader.RequestUpdateInventoryItem(remoteClient, transactionID, item);
        }
    }
}