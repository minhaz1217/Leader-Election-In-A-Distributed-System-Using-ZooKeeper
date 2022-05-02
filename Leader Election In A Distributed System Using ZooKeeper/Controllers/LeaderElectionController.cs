using Leader_Election_In_A_Distributed_System_Using_ZooKeeper.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Leader_Election_In_A_Distributed_System_Using_ZooKeeper.Controllers
{

    [ApiController]
    [Route("/api/[controller]")]
    public class LeaderElectionController : ControllerBase
    {
        private readonly LeaderElectionService _leaderElectionService;

        public LeaderElectionController(LeaderElectionService leaderElectionService)
        {
            _leaderElectionService = leaderElectionService;
        }

        [HttpGet("is-leader")]
        public async Task<IActionResult> IsLeaderAsync()
        {
            var isLeader = await _leaderElectionService.IsLeaderAsync("my-unique-service");
            return Ok(string.Format("{0} - {1}", Dns.GetHostName(), isLeader));
        }
    }
}
