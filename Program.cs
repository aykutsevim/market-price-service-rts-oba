using System;
using System.Text.Json;
using System.Threading;
using StackExchange.Redis;

class Program
{
    static void Main()
    {
        //string redisConnectionString = "redis-11850.c281.us-east-1-2.ec2.cloud.redislabs.com:11850,password=RIQxfcAullMkJ8THWZEQNILlHaYUR8aJ,ssl=False,abortConnect=False,connectTimeout=5000,syncTimeout=5000,defaultDatabase=0,connectRetry=10";
        string redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        if (redisConnectionString == null)
        {
            redisConnectionString = "redis-11850.c281.us-east-1-2.ec2.cloud.redislabs.com:11850,password=RIQxfcAullMkJ8THWZEQNILlHaYUR8aJ,ssl=False,abortConnect=False,connectTimeout=5000,syncTimeout=5000,defaultDatabase=0,connectRetry=10";
        }

        string channelName = "priceChannel";
        string priceRangeKey = "priceRange";
        string marketAlphaKey = "marketAlpha";
        string marketBetaKey = "marketBeta";

        // Connect to Redis
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConnectionString);
        ISubscriber subscriber = redis.GetSubscriber();

        // Generate and publish the current price every second
        Timer timer = new Timer(_ =>
        {
            // Fetch Max and Min Prices from Redis key
            //Tuple<double, double> priceRange = GetPriceRange(redis.GetDatabase(), priceRangeKey);
            var redisDB = redis.GetDatabase();

            double currentTime = (DateTime.Now - DateTime.Today).TotalSeconds;
            decimal currentPrice = GenerateCurrentPrice(currentTime, 15.0f, 25.0f);

            decimal marketAlphaBuyPrice = GenerateCurrentPrice(currentTime, 15.0f, 25.0f);
            decimal marketAlphaSellPrice = GenerateCurrentPrice(currentTime - 30.1f, 20.0f, 30.0f);
            decimal marketBetaBuyPrice = GenerateCurrentPrice(currentTime - 2000.0f, 15.0f, 25.0f);
            decimal marketBetaSellPrice = GenerateCurrentPrice(currentTime - 2500.0f, 20.0f, 30.0f);

            redisDB.HashSet(marketAlphaKey, "BuyPrice", marketAlphaBuyPrice.ToString());
            redisDB.HashSet(marketAlphaKey, "SellPrice", marketAlphaSellPrice.ToString());
            redisDB.HashSet(marketBetaKey, "BuyPrice", marketBetaBuyPrice.ToString());
            redisDB.HashSet(marketBetaKey, "SellPrice", marketBetaSellPrice.ToString());
            // Generate artificial current price based on a sine wave function

            // Publish the current price to the channel
            var event1 = new MarketPriceUpdatedEvent
            {
                EventType = EventTypeEnum.MarketPriceChanged,
                MarketName = "MarketAlpha",
                BuyPrice = marketAlphaBuyPrice,
                SellPrice = marketAlphaSellPrice,
            };

            var event2 = new MarketPriceUpdatedEvent
            {
                EventType = EventTypeEnum.MarketPriceChanged,
                MarketName = "MarketBeta",
                BuyPrice = marketBetaBuyPrice,
                SellPrice = marketBetaSellPrice,
            };

            var event1string = JsonSerializer.Serialize(event1);
            var event2string = JsonSerializer.Serialize(event2);

            try
            {
                Console.WriteLine($"To :{channelName}. Send:\r\n{event1string}");
                _ = subscriber.Publish(channelName, JsonSerializer.Serialize(event1));

                Console.WriteLine($"To :{channelName}. Send:\r\n{event2string}");
                _ = subscriber.Publish(channelName, JsonSerializer.Serialize(event2));
            }
            catch (StackExchange.Redis.RedisTimeoutException exception)
            {
                Console.WriteLine(exception.Message);
            }

        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        Console.WriteLine($"Publishing generated prices to Redis channel: {channelName}. Press Enter to exit.");

        while (true)
        {
            Thread.Sleep(1000);
        }

    }

    static Tuple<double, double> GetPriceRange(IDatabase redisDatabase, string key)
    {
        // Fetch Max and Min Prices from Redis key
        var maxPrice = Convert.ToDouble(redisDatabase.HashGet(key, "MaxPrice"));
        var minPrice = Convert.ToDouble(redisDatabase.HashGet(key, "MinPrice"));

        return Tuple.Create(maxPrice, minPrice);
    }

    static decimal GenerateCurrentPrice(double time, double maxPrice, double minPrice)
    {
        // Generate current price using a sine wave function between max and min prices
        double amplitude = (maxPrice - minPrice) / 2;
        double frequency = 0.0002f; // Adjust the frequency based on your requirement

        decimal currentPrice = (decimal) (minPrice + amplitude * Math.Sin(2 * Math.PI * frequency * time));

        return Math.Round(currentPrice, 2); // Round to 2 decimal places
    }
}
