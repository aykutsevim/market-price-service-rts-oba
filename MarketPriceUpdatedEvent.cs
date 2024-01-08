using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum EventTypeEnum
{
    MarketPriceChanged,
}
public class MarketPriceUpdatedEvent
{
    public EventTypeEnum EventType { get; set; }
    public string MarketName { get; set; } = String.Empty;
    public Decimal BuyPrice { get; set; }
    public Decimal SellPrice { get; set; }
}
