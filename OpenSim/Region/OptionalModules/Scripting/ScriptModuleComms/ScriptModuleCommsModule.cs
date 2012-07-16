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
using System.Reflection;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Mono.Addins;
using OpenMetaverse;
using System.Linq;
using System.Linq.Expressions;

namespace OpenSim.Region.OptionalModules.Scripting.ScriptModuleComms
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "ScriptModuleCommsModule")]
    class ScriptModuleCommsModule : INonSharedRegionModule, IScriptModuleComms
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

#region ScriptInvocation
        protected class ScriptInvocationData
        {
            public Delegate ScriptInvocationDelegate { get; private set; }
            public string FunctionName { get; private set; }
            public Type[] TypeSignature { get; private set; }
            public Type ReturnType { get; private set; }

            public ScriptInvocationData(string fname, Delegate fn, Type[] callsig, Type returnsig)
            {
                FunctionName = fname;
                ScriptInvocationDelegate = fn;
                TypeSignature = callsig;
                ReturnType = returnsig;
            }
        }

        private Dictionary<string,ScriptInvocationData> m_scriptInvocation = new Dictionary<string,ScriptInvocationData>();
#endregion

        private IScriptModule m_scriptModule = null;
        public event ScriptCommand OnScriptCommand;

#region RegionModuleInterface
        public void Initialise(IConfigSource config)
        {
        }

        public void AddRegion(Scene scene)
        {
            scene.RegisterModuleInterface<IScriptModuleComms>(this);
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            m_scriptModule = scene.RequestModuleInterface<IScriptModule>();
            
            if (m_scriptModule != null)
                m_log.Info("[MODULE COMMANDS]: Script engine found, module active");
        }

        public string Name
        {
            get { return "ScriptModuleCommsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }
#endregion

#region ScriptModuleComms

        public void RaiseEvent(UUID script, string id, string module, string command, string k)
        {
            ScriptCommand c = OnScriptCommand;

            if (c == null)
                return;

            c(script, id, module, command, k);
        }

        public void DispatchReply(UUID script, int code, string text, string k)
        {
            if (m_scriptModule == null)
                return;

            Object[] args = new Object[] {-1, code, text, k};

            m_scriptModule.PostScriptEvent(script, "link_message", args);
        }

        public void RegisterScriptInvocation(object target, string meth)
        {
            MethodInfo mi = target.GetType().GetMethod(meth,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (mi == null)
            {
                m_log.WarnFormat("[MODULE COMMANDS] Failed to register method {0}",meth);
                return;
            }

            RegisterScriptInvocation(target, mi);
        }

        public void RegisterScriptInvocation(object target, string[] meth)
        {
            foreach (string m in meth)
                RegisterScriptInvocation(target, m);
        }

        public void RegisterScriptInvocation(object target, MethodInfo mi)
        {
            m_log.DebugFormat("[MODULE COMMANDS] Register method {0} from type {1}", mi.Name, target.GetType().Name);

            Type delegateType;
            var typeArgs = mi.GetParameters()
                    .Select(p => p.ParameterType)
                    .ToList();

            if (mi.ReturnType == typeof(void))
            {
                    delegateType = Expression.GetActionType(typeArgs.ToArray());
            }
            else
            {
                    typeArgs.Add(mi.ReturnType);
                    delegateType = Expression.GetFuncType(typeArgs.ToArray());
            }

            Delegate fcall = Delegate.CreateDelegate(delegateType, target, mi);

            lock (m_scriptInvocation)
            {
                ParameterInfo[] parameters = fcall.Method.GetParameters ();
                if (parameters.Length < 2) // Must have two UUID params
                    return;

                // Hide the first two parameters
                Type[] parmTypes = new Type[parameters.Length - 2];
                for (int i = 2 ; i < parameters.Length ; i++)
                    parmTypes[i - 2] = parameters[i].ParameterType;
                m_scriptInvocation[fcall.Method.Name] = new ScriptInvocationData(fcall.Method.Name, fcall, parmTypes, fcall.Method.ReturnType);
            }
        }
        
        public Delegate[] GetScriptInvocationList()
        {
            List<Delegate> ret = new List<Delegate>();

            lock (m_scriptInvocation)
            {
                foreach (ScriptInvocationData d in m_scriptInvocation.Values)
                    ret.Add(d.ScriptInvocationDelegate);
            }
            return ret.ToArray();
        }

        public string LookupModInvocation(string fname)
        {
            lock (m_scriptInvocation)
            {
                ScriptInvocationData sid;
                if (m_scriptInvocation.TryGetValue(fname,out sid))
                {
                    if (sid.ReturnType == typeof(string))
                        return "modInvokeS";
                    else if (sid.ReturnType == typeof(int))
                        return "modInvokeI";
                    else if (sid.ReturnType == typeof(float))
                        return "modInvokeF";
                    else if (sid.ReturnType == typeof(UUID))
                        return "modInvokeK";
                    else if (sid.ReturnType == typeof(OpenMetaverse.Vector3))
                        return "modInvokeV";
                    else if (sid.ReturnType == typeof(OpenMetaverse.Quaternion))
                        return "modInvokeR";
                    else if (sid.ReturnType == typeof(object[]))
                        return "modInvokeL";

                    m_log.WarnFormat("[MODULE COMMANDS] failed to find match for {0} with return type {1}",fname,sid.ReturnType.Name);
                }
            }

            return null;
        }

        public Delegate LookupScriptInvocation(string fname)
        {
            lock (m_scriptInvocation)
            {
                ScriptInvocationData sid;
                if (m_scriptInvocation.TryGetValue(fname,out sid))
                    return sid.ScriptInvocationDelegate;
            }

            return null;
        }

        public Type[] LookupTypeSignature(string fname)
        {
            lock (m_scriptInvocation)
            {
                ScriptInvocationData sid;
                if (m_scriptInvocation.TryGetValue(fname,out sid))
                    return sid.TypeSignature;
            }

            return null;
        }

        public Type LookupReturnType(string fname)
        {
            lock (m_scriptInvocation)
            {
                ScriptInvocationData sid;
                if (m_scriptInvocation.TryGetValue(fname,out sid))
                    return sid.ReturnType;
            }

            return null;
        }

        public object InvokeOperation(UUID hostid, UUID scriptid, string fname, params object[] parms)
        {
            List<object> olist = new List<object>();
            olist.Add(hostid);
            olist.Add(scriptid);
            foreach (object o in parms)
                olist.Add(o);
            Delegate fn = LookupScriptInvocation(fname);
            return fn.DynamicInvoke(olist.ToArray());
        }
#endregion

    }
}
