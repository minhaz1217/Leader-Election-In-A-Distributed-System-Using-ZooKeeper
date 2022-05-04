using org.apache.zookeeper;
using System.Collections.ObjectModel;
using System.Text;
using static org.apache.zookeeper.Watcher.Event;
using static org.apache.zookeeper.ZooDefs;

namespace Leader_Election_In_A_Distributed_System_Using_ZooKeeper.Services
{
    public class LeaderElectionService : Watcher
    {
        private readonly string _leaderElectionPath;
        private readonly ZooKeeperClientService _zooKeeperClientService;
        private readonly string _znodeId;
        private readonly IDictionary<string, bool> _isLeader;
        private readonly ICollection<string> _serviceAdded;
        private bool _leaderCheckReady = false;
        public LeaderElectionService(ZooKeeperClientService zooKeeperClientService)
        {
            _leaderElectionPath = "/ELECTION";

            _zooKeeperClientService = zooKeeperClientService;
            _zooKeeperClientService.Connect(this);
            _znodeId = Guid.NewGuid().ToString();
            _isLeader = new Dictionary<string, bool>(); // in this we maintain our last know status
            _serviceAdded = new Collection<string>(); // we use this to maintian the list of nodes that we've already added.
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
                    await AddRootNode();
                }

                var servicePath = $"{_leaderElectionPath}/{service}";
                await AddServicePath(servicePath);


                var childNodes = (await _zooKeeperClientService.zooKeeper.getChildrenAsync(servicePath)).Children.OrderBy(x => x);

                var leadChild = await _zooKeeperClientService.zooKeeper.getDataAsync($"{servicePath}/{childNodes.First()}", true);
                var leaderData = Encoding.UTF8.GetString(leadChild.Data);

                _isLeader[service] = leaderData == _znodeId;

                return _isLeader[service];
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task AddServicePath(string servicePath)
        {
            if (!_serviceAdded.Any(x => x == servicePath))
            {
                var serviceNode = await _zooKeeperClientService.zooKeeper.existsAsync(servicePath);
                if (serviceNode == null)
                {
                    await _zooKeeperClientService.zooKeeper.createAsync(servicePath, Encoding.UTF8.GetBytes(servicePath), Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                }

                await _zooKeeperClientService.zooKeeper.createAsync($"{servicePath}/n_", Encoding.UTF8.GetBytes(_znodeId), Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
                _serviceAdded.Add(servicePath);
            }
        }

        private async Task AddRootNode()
        {
            var rootNode = await _zooKeeperClientService.zooKeeper.existsAsync(_leaderElectionPath);
            if (rootNode == null)
            {
                await _zooKeeperClientService.zooKeeper.createAsync(_leaderElectionPath, Encoding.UTF8.GetBytes(_leaderElectionPath), Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
            }
            _leaderCheckReady = true;
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
                            _leaderCheckReady = false;
                            _isLeader.Clear();
                            await _zooKeeperClientService.Connect(this);
                            break;
                    }
                    break;
            }
        }
    }
}
