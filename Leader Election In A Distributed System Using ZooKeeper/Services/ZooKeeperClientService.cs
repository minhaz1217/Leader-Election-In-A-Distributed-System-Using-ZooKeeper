using Microsoft.AspNetCore.Mvc;
using org.apache.zookeeper;
using System.Collections.ObjectModel;
using System.Text;
using static org.apache.zookeeper.Watcher.Event;
using static org.apache.zookeeper.ZooDefs;

namespace Leader_Election_In_A_Distributed_System_Using_ZooKeeper.Services
{
    public class ZooKeeperClientService : Watcher
    {


        private ZooKeeper _zooKeeper;
        private readonly string _uniqueGuid;
        private readonly string _nodeName;
        private readonly IDictionary<string, bool> _isLeader;
        private readonly ICollection<string> _nodeAdded;
        private bool _leaderCheckReady = false;


        public ZooKeeperClientService()
        {
            _nodeName = "/leader-selection";
            _uniqueGuid = Guid.NewGuid().ToString();
            _isLeader = new Dictionary<string, bool>(); // in this we maintain our last know status
            _nodeAdded = new Collection<string>(); // we use this to maintian the list of nodes that we've already added.
            Connect();
        }

        public async Task<bool> IsLeaderAsync(string service)
        {
            if (_isLeader.Any() && _isLeader.TryGetValue(service, out var result))
            {
                return result;
            }
            return await CheckLeaderAsync(service);
        }

        public async Task<bool> CheckLeaderAsync(string service)
        {
            try
            {
                if (!_leaderCheckReady)
                {
                    return false;
                }

                var path = $"{_nodeName}/{service}";
                if (!_nodeAdded.Any(x => x == service))
                {
                    var tenantNode = await _zooKeeper.existsAsync(path);
                    if (tenantNode == null)
                    {
                        await _zooKeeper.createAsync(path, Encoding.UTF8.GetBytes(path), Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                    }

                    await _zooKeeper.createAsync($"{path}/n_", Encoding.UTF8.GetBytes(_uniqueGuid), Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
                    _nodeAdded.Add(service);
                }
                var childNodes = (await _zooKeeper.getChildrenAsync(path)).Children.OrderBy(x => x);

                var leadChild = await _zooKeeper.getDataAsync($"{path}/{childNodes.First()}", true);
                var leaderData = Encoding.UTF8.GetString(leadChild.Data);
                _isLeader[service] = leaderData == _uniqueGuid;
                return _isLeader[service];


            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task AddRootNode()
        {
            var rootNode = await _zooKeeper.existsAsync(_nodeName);
            if (rootNode == null)
            {
                await _zooKeeper.createAsync(_nodeName, Encoding.UTF8.GetBytes(_nodeName), Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
            }
            _leaderCheckReady = true;
        }


        #region Zookeeper connection

        public Task Connect()
        {
            try
            {
                _zooKeeper = new ZooKeeper("localhost:4001", 30000, this);
            }
            catch (Exception ex)
            {
                return Task.CompletedTask;
            }
            return Task.CompletedTask; ;
        }

        public async Task Disconnect()
        {
            if (_zooKeeper != null)
            {
                await _zooKeeper.closeAsync();
            }
        }


        public override async Task process(WatchedEvent @event)
        {
            switch (@event.get_Type())
            {
                case EventType.NodeDeleted:
                    _isLeader.Clear();
                    break;
                case EventType.None:
                    switch (@event.getState())
                    {
                        case KeeperState.SyncConnected:
                            await AddRootNode();
                            break;
                        case KeeperState.Disconnected:
                            _isLeader.Clear();
                            await Connect();
                            break;
                    }
                    break;
            }
        }

        #endregion Zookeeper connection

    }
}
