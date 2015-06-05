﻿/*
 * Copyright (C) 2015, Cinder Roxley <cinder@sdf.org>
 * 
 * Permission is hereby granted, free of charge, to any person or organization
 * obtaining a copy of the software and accompanying documentation covered by
 * this license (the "Software") to use, reproduce, display, distribute,
 * execute, and transmit the Software, and to prepare derivative works of the
 * Software, and to permit third-parties to whom the Software is furnished to
 * do so, all subject to the following:
 * 
 * The copyright notices in the Software and this entire statement, including
 * the above license grant, this restriction and the following disclaimer,
 * must be included in all copies of the Software, in whole or in part, and
 * all derivative works of the Software, unless such copies or derivative
 * works are solely in the form of machine-executable object code generated by
 * a source language processor.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
 * SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
 * FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using OpenMetaverse;
using OpenSim.Framework;
using MySql.Data.MySqlClient;

namespace OpenSim.Data.MySQL
{
    public class MySQLAgentPreferencesData : MySQLGenericTableHandler<AgentPreferencesData>, IAgentPreferencesData
    {
        public MySQLAgentPreferencesData(string connectionString, string realm)
            : base(connectionString, realm, "AgentPrefs")
        {
        }

        public AgentPreferencesData GetPrefs(UUID agentID)
        {
            AgentPreferencesData[] ret = Get("PrincipalID", agentID.ToString());

            if (ret.Length == 0)
                return null;

            return ret[0];
        }

        public void StorePrefs(AgentPreferencesData data)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.CommandText = String.Format("replace into `{0}` (`PrincipalID`, `AccessPrefs`, `HoverHeight`, `Language`, `LanguageIsPublic`, `PermEveryone`, `PermGroup`, `PermNextOwner`) VALUES (?Principal, ?AP, ?HH, ?Lang, ?LIP, ?PE, ?PG, ?PNO)", m_Realm);
                cmd.Parameters.AddWithValue("?Principal", data.PrincipalID.ToString());
                cmd.Parameters.AddWithValue("?AP", data.AccessPrefs);
                cmd.Parameters.AddWithValue("?HH", data.HoverHeight);
                cmd.Parameters.AddWithValue("?Lang", data.Language);
                cmd.Parameters.AddWithValue("?LIP", data.LanguageIsPublic);
                cmd.Parameters.AddWithValue("?PE", data.PermEveryone);
                cmd.Parameters.AddWithValue("?PG", data.PermGroup);
                cmd.Parameters.AddWithValue("?PNO", data.PermNextOwner);

                ExecuteNonQuery(cmd);
            }
        }
    }
}
