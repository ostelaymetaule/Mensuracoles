using System;

namespace Mensuracoles.Repository
{
    public class UserMeasurement
    {
        public int MessageId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string BinName { get; set; }
        public long ChatId { get; set; }

        public DateTime DataPointTimestamp { get; set; } = DateTime.Now;
        public decimal Data { get; set; }

    }
}
 
