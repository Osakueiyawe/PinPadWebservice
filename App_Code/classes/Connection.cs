using System.Configuration;

namespace GTMTPaymentEngine
{
    class Connection
    {
        string connectionOracle, connectionSQL, tokenconnection;

        #region Connection Constructor

        public Connection()
        {
            connectionOracle = ConfigurationManager.AppSettings["BASISConString"];
            connectionSQL = ConfigurationManager.AppSettings["e_oneConnStr"];
            tokenconnection = ConfigurationManager.AppSettings["tokenConnStr"];
        }
        #endregion

        #region Connection properties

        public string ConnectionOracle
        {
            get
            {
                return connectionOracle;
            }
        }

        public string ConnectionSQL
        {
            get
            {
                return connectionSQL;
            }
        }

        public string TokenConnection
        {
            get
            {
                return tokenconnection;
            }
        }
        #endregion
    }
}
