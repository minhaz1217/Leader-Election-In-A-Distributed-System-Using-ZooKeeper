using Microsoft.AspNetCore.Mvc;
using org.apache.zookeeper;
using System.Collections.ObjectModel;
using System.Text;
using static org.apache.zookeeper.Watcher.Event;
using static org.apache.zookeeper.ZooDefs;

namespace Leader_Election_In_A_Distributed_System_Using_ZooKeeper.Services
{
    public class ZooKeeperClientService
    {
        public ZooKeeper zooKeeper;
        private readonly IConfiguration _configuration;
        public ZooKeeperClientService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        #region Zookeeper connection

        public Task Connect(Watcher watcher)
        {
            try
            {
                zooKeeper = new ZooKeeper(_configuration["zookeeper_connection_string"],  int.Parse(_configuration["zookeeper_connection_timeout"]) , watcher);
            }
            catch (Exception ex)
            {
            }
            return Task.CompletedTask;
        }

        public async Task Disconnect()
        {
            if (zooKeeper != null)
            {
                await zooKeeper.closeAsync();
            }
        }
        #endregion Zookeeper connection

    }
}
