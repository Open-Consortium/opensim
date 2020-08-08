using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using Caps = OpenSim.Framework.Capabilities.Caps;

namespace OpenSim.Region.ClientStack.Linden
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "LSLSyntax")]
    public class LSLSyntaxModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool enabled = false;
        private UUID m_SyntaxID = UUID.Zero;
        private string m_SyntaxDir = "./LSLSyntax/";

        private string m_LSLSyntaxURL = "localhost";

        #region IRegionModuleBase implementation

        public void Initialise(IConfigSource config)
        {
            IConfig cnf = config.Configs["ClientStack.LindenCaps"];
            if (cnf == null)
                return;

            m_LSLSyntaxURL = cnf.GetString("Cap_LSLSyntax", string.Empty);

            cnf = config.Configs["LSLSyntax"];

            if (cnf == null)
                return;

            string key = cnf.GetString("SyntaxID", string.Empty);
            if (key != string.Empty)
            {
                UUID.TryParse(key, out m_SyntaxID);
            }

            m_SyntaxDir = cnf.GetString("SyntaxDir", m_SyntaxDir);

            enabled = m_LSLSyntaxURL != string.Empty && m_SyntaxID != UUID.Zero;

            if(enabled) m_log.Info("[LSLSyntax] Plugin enabled!");
        }

        public void AddRegion(Scene scene)
        {
            if (!enabled)
                return;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!enabled)
                return;

            ISimulatorFeaturesModule featuresModule = scene.RequestModuleInterface<ISimulatorFeaturesModule>();

            if (featuresModule != null)
                featuresModule.OnSimulatorFeaturesRequest += OnSimulatorFeaturesRequest;

            scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void RemoveRegion(Scene scene)
        {
            if (!enabled)
                return;
        }

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "LSLSyntaxModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        #endregion

        #region Event Handlers
        private void OnSimulatorFeaturesRequest(UUID agentID, ref OSDMap features)
        {
            features["LSLSyntaxId"] = new OSDUUID(m_SyntaxID);
        }

        public void RegisterCaps(UUID agentID, Caps caps)
        {
            if (m_LSLSyntaxURL == "localhost")
            {
                caps.RegisterSimpleHandler("LSLSyntax", new SimpleStreamHandler("/" + UUID.Random(), HandleLSLSyntaxRequest));
            }
            else if(m_LSLSyntaxURL != string.Empty)
            {
                caps.RegisterHandler("LSLSyntax", m_LSLSyntaxURL);
            }
        }
        #endregion

        #region Cap Handles
        private void HandleLSLSyntaxRequest(IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            if (httpRequest.HttpMethod != "GET")
            {
                httpResponse.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            httpResponse.RawBuffer = File.ReadAllBytes(m_SyntaxDir + m_SyntaxID.ToString() + ".xml");
            httpResponse.StatusCode = (int)HttpStatusCode.OK;
        }
        #endregion
    }
}
