using Cassandra;
using Cassandra.Mapping;
using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace CassandraQuickStartSample
{
    public class Program
    {
        // Edited by: Subhasish Ghosh
        // Date: 16 Oct 2021
        // Location: Bengaluru
        // Based on CassandraQuickStartSample (GitHub); modifications added with sample code.
        
        // Cassandra Cluster Configs      
        private const string UserName = "<<ENTER USER NAME HERE>>"; 
        private const string Password = "<<ENTER PASSWORD HERE>>";
        private const string CassandraContactPoint = "<<ENTER CONTACT POINT HERE>>";  // DnsName  
        private static int CassandraPort = 10350;

        public static void Main(string[] args)
        {
            // Connect to cassandra cluster  (Cassandra API on Azure Cosmos DB supports only TLSv1.2)
            var options = new Cassandra.SSLOptions(SslProtocols.Tls12, true, ValidateServerCertificate);
            options.SetHostNameResolver((ipAddress) => CassandraContactPoint);
            Cluster cluster = Cluster.Builder().WithCredentials(UserName, Password).WithPort(CassandraPort).AddContactPoint(CassandraContactPoint).WithSSL(options).Build();
            ISession session = cluster.Connect();

            // Creating Cassandra KeySpace: "uprofile" with provisioned-RU 2000 set at "keyspace level".
            session.Execute("DROP KEYSPACE IF EXISTS uprofile");
            session.Execute("CREATE KEYSPACE uprofile WITH REPLICATION = { 'class' : 'NetworkTopologyStrategy', 'datacenter1' : 1 } AND cosmosdb_provisioned_throughput = 2000;");
            Console.WriteLine(String.Format("Successfully created keyspace uprofile with 2000 RU at keyspace level."));

            // Creating Cassandra KeySpace: "weather". Am not creating throughput (RU) at keyspace level; but will later set at 'table level' when creating table.
            session.Execute("DROP KEYSPACE IF EXISTS weather");
            session.Execute("CREATE KEYSPACE weather WITH REPLICATION = { 'class' : 'SimpleStrategy' };");
            Console.WriteLine(String.Format("Successfully created keyspace weather."));

            // *********** Creating Cassandra Tables *********** 

            // A PartitionKey defines Data Distribution.

            // A Primary Key in Cassandra is atleast 1 ParitionKey + (0) OR More Clustering Columns.
            // Best Practices - Rules of thumb: 1) must ensure uniqueness.
            //                                  2) more the cardinality, the better.
            //                                  3) store together what you retrieve together.
            //                                  4) may define sorting (if needed).
            //                                  5) ***avoid*** large partitions.
            //                                  6) ***avoid*** hot partitions (uneven distribution of data resulting in some Partitions getting more Qd than other).

            // Apache Cassandra (OSS) version has 100MB Limit Size for 1 PartitionKey; CosmosDB Cassandra API has 20GB Limit Size for 1 PartitionKey.
            // No storage limit - can store GBs, TBs, PBs.
            // Key differences can be studied here > https://docs.microsoft.com/en-us/azure/cosmos-db/cassandra/cassandra-faq#key-differences-between-apache-cassandra-and-the-cassandra-api

            // option #1: table with single primary key for *********** uprofile***********.
            session.Execute("CREATE TABLE IF NOT EXISTS uprofile.user (user_id int PRIMARY KEY, user_name text, user_bcity text)");
            Console.WriteLine(String.Format("Successfully created table uprofile.user with SINGLE Primary Key."));

            // Creating 2 tables for holding Weather Data for *********** weather***********.

            // option #1: table with single primary key.
            // session.Execute("CREATE TABLE IF NOT EXISTS weather.data (station_id text PRIMARY, temp int, ts time)");
            // Console.WriteLine(String.Format("Successfully created table data with SINGLE Primary Key."));

            // Let us assume we have a use-case wherein we are collating Weather Data from different stations all over the country and then storing them.
            // After storing them, if we wish to query them using StationID. Then when we create a table like above (i.e. Single Primary Key), this approach
            // *not* work. As new temperature records flow in, the data from each station will be overwritten.
            // Hence, we will need to use a Compound Primary Key.
            session.Execute("CREATE TABLE IF NOT EXISTS weather.data (station_id int, identity_id int, temp int, state text, PRIMARY KEY (station_id, identity_id)) WITH cosmosdb_provisioned_throughput = 4000 AND CLUSTERING ORDER BY (identity_id DESC)");
            // station_id = partition key which means all data from a specific station will reside within a single partition
            // for illustration purposes, I've taken dt as "text" datatype. In real-life scenario, it is recommended to take dt as "date" datatype with clustering column based on which
            // the rows within each partition will be sorted in descending order (time OR timestamp is better choice for CARDINALITY in Cassandra DB).
            // NB: You can only insert values smaller than 64 kB into a clustering column.
            Console.WriteLine(String.Format("Successfully created table weather.data with COMPOUND Primary Key + provisioned throughput = 4000 RUs."));

            Console.WriteLine(String.Format("Starting inserting data into uprofile.user table..."));

            // For keyspace: uprofile
            session = cluster.Connect("uprofile");
            IMapper mapper1 = new Mapper(session);

            // Inserting Data into user table
            mapper1.Insert<User>(new User(1, "LyubovK", "Dubai"));
            mapper1.Insert<User>(new User(2, "JiriK", "Toronto"));
            mapper1.Insert<User>(new User(3, "IvanH", "Mumbai"));
            mapper1.Insert<User>(new User(4, "LiliyaB", "Seattle"));
            mapper1.Insert<User>(new User(5, "JindrichH", "Buenos Aires"));
            mapper1.Insert<User>(new User(6, "HainH", "Oslo"));
            mapper1.Insert<User>(new User(7, "SubhasishG", "Kolkata"));
            mapper1.Insert<User>(new User(8, "AbhijitS", "Mumbai"));
            mapper1.Insert<User>(new User(9, "PoornimaS", "Chennai"));
            mapper1.Insert<User>(new User(10, "NimeshJ", "London"));
            mapper1.Insert<User>(new User(11, "PradipVS", "New Delhi"));
            mapper1.Insert<User>(new User(12, "SauravN", "Gurgaon"));
            mapper1.Insert<User>(new User(13, "ArunL", "Paris"));
            mapper1.Insert<User>(new User(14, "BijoyM", "New York"));
            mapper1.Insert<User>(new User(15, "HeinkelL", "Deoghar"));
            mapper1.Insert<User>(new User(16, "DragoL", "Baku"));
            mapper1.Insert<User>(new User(17, "HerbertS", "Lonavala"));
            mapper1.Insert<User>(new User(18, "AnjaliS", "Beijing"));
            mapper1.Insert<User>(new User(19, "RituS", "Tokyo"));
            mapper1.Insert<User>(new User(20, "AndreiB", "Moscow"));
            Console.WriteLine("Successfully inserted data into uprofile.user table.");

            Console.WriteLine(String.Format("Starting inserting data into weather.data table..."));

            // For keyspace: weather
            session = cluster.Connect("weather");
            IMapper mapper2 = new Mapper(session);

            mapper2.Insert<Data>(new Data(1, 20211015, 70, "andhrapradesh"));
            mapper2.Insert<Data>(new Data(2, 20211014, 71, "arunachalpradesh"));
            mapper2.Insert<Data>(new Data(3, 20211013, 72, "assam"));
            mapper2.Insert<Data>(new Data(4, 20210901, 73, "bihar"));
            mapper2.Insert<Data>(new Data(5, 20210715, 74, "goa"));
            mapper2.Insert<Data>(new Data(6, 20210115, 75, "gujrat"));
            mapper2.Insert<Data>(new Data(7, 20210116, 76, "haryana"));
            mapper2.Insert<Data>(new Data(8, 20210117, 77, "jharkhand"));
            mapper2.Insert<Data>(new Data(9, 20210217, 70, "karnataka"));
            mapper2.Insert<Data>(new Data(10, 20210318, 69, "karnataka"));
            mapper2.Insert<Data>(new Data(11, 20191119, 81, "kerala"));
            mapper2.Insert<Data>(new Data(12, 20210320, 81, "kerala"));
            mapper2.Insert<Data>(new Data(13, 20210221, 82, "westbengal"));
            mapper2.Insert<Data>(new Data(14, 20210423, 83, "tamilnadu"));
            mapper2.Insert<Data>(new Data(15, 20210424, 84, "maharashtra"));
            mapper2.Insert<Data>(new Data(16, 20191231, 71, "nagaland"));
            mapper2.Insert<Data>(new Data(17, 20210210, 45, "jammu"));
            mapper2.Insert<Data>(new Data(18, 20190808, 46, "odisha"));
            mapper2.Insert<Data>(new Data(19, 20190809, 47, "telangana"));
            mapper2.Insert<Data>(new Data(20, 20190919, 91, "uttarpradesh"));
            Console.WriteLine("Successfully inserted data into weather.data table.");

            Console.WriteLine("Hurrah! This successfully exhibits creating a Cassandra Data Model in Azure Cosmos DB using .NET SDK, loading data into Keyspace Tables successfully.");
            Console.WriteLine("Proceed for Querying Operations.");

            Console.ReadLine();

            // Basic Queries
            Console.WriteLine("Select ALL for Keyspace uprofile:");
            Console.WriteLine("--------------------------------------------------------------");
            foreach (User user in mapper1.Fetch<User>("Select * from uprofile.user"))
            {
                Console.WriteLine(user);
            }

            Console.ReadLine();

            Console.WriteLine("Getting by id 3");
            Console.WriteLine("--------------------------------------------------------------");
            User userId3 = mapper1.FirstOrDefault<User>("Select * from uprofile.user where user_id = ?", 3);
            Console.WriteLine(userId3);

            Console.ReadLine();

            // Keyspace: Weather: We can query the table for data corresponding to a particular station since it's a part of the primary key.
            Console.WriteLine("Query table by filtering against particular station:");
            Console.WriteLine("--------------------------------------------------------------");
            Data query1 = mapper2.FirstOrDefault<Data>("Select * from data where station_id = ?", 1);
            Console.WriteLine(query1);

            Console.ReadLine();

            // Keyspace: Weather: Let us try and query the table for data by filtering by non-primary key.
            // BAD PRACTICE: To Avoid.
            // It's not advisable to execute filter queries on the columns that aren't partitioned.
            // We can use ALLOW FILTERING explicitly, but that results in an operation that may not perform well.
            // The below code snippet will fail resulting in an Error since Filtering Against Non-Primary Key is NOT ALLOWED in Apache Cassandra Database.
            Console.WriteLine("Query table by filtering against Non-Primary Key:");
            Console.WriteLine("--------------------------------------------------------------");
            Data query2 = mapper2.FirstOrDefault<Data>("Select * from weather.data where identity_id = ?", 20210318);
            Console.WriteLine(query2);

            // Solution to problem above: YOU CAN ADD AN INDEX.
            // Difference: In CosmosDB CoreSQL API, all attributes are "indexed" by default.
            // In CosmosDB Cassandra API, you can choose which specific attributes you want to Index; this is called 'Concept of Secondary Indexes'.
            // CREATE INDEX ON weather.data (state); describe table weather.data;

            // Clean up of Table and KeySpace
            // session.Execute("DROP table user");
            // session.Execute("DROP KEYSPACE uprofile");

            // Wait for enter key before exiting  
            Console.ReadLine();
        }

        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

             Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }
    }
}