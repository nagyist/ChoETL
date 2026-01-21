using System;
using System.Collections.Generic;
using System.Diagnostics;
using ChoETL;
using System.Linq;

namespace ChoETL.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = TraceLevel.Off;
            QuoteFieldValuesTest();
            return;

            CustomerLoadTest();
            //CSV2JSON();
        }

        static void QuoteFieldValuesTest()
        {
            string csv = @"V BELT,30-1/32""|19.44||12.69";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithDelimiter("|")
                .Configure(c => c.QuoteChar = '#')
                )
            {
                r.ToArray().Print();
            }
        }
        public class Customer
        {
            [ChoCSVRecordField()]
            public int Index { get; set; }
            [ChoCSVRecordField(FieldName = "Customer Id")]
            public string CustomerId { get; set; }
            [ChoCSVRecordField(FieldName = "First Name")]
            public string FirstName { get; set; }
            [ChoCSVRecordField(FieldName = "Last Name")]
            public string LastName { get; set; }
            [ChoCSVRecordField]
            public string Company { get; set; }
            [ChoCSVRecordField]
            public string City { get; set; }
            [ChoCSVRecordField]
            public string Country { get; set; }
        }
        public static void CustomerLoadTest()
        {
            //string csv = @"C:\Users\nraj39\Downloads\customers-10000.csv";
            string csv = @"C:\Users\nraj39\Downloads\customers-2000000.csv";

            for (var i = 0; i < 10; i++)
            {
                using (var r = new ChoCSVReader<Customer>(csv)
                    .WithFirstLineHeader()
                    .NotifyAfter(1000)
                    .Setup(s => s.RowsLoaded += (o, e) =>
                    {
                        //$"Rows read: {e.RowsLoaded}.".Print();
                        if (e.RowsLoaded == 1000000)
                            e.Abort = true;
                    })
                    .Setup(s => s.BeforeRecordLoad += (o, e) =>
                    {
                        var customer = e.Record as Customer;
                        var payload = e.Payload as string[];

                        if (payload != null)
                        {
                            customer.Index = int.Parse(payload[0]); //.CastTo<int>();
                            customer.CustomerId = payload[1];
                            customer.FirstName = payload[2];
                            customer.LastName = payload[3];
                            customer.Company = payload[4];
                            customer.City = payload[5];
                            customer.Country = payload[6];
                        }
                        e.Handled = true;
                    })
                    )
                {
                    Stopwatch w = Stopwatch.StartNew();
                    var customers = r.ToList();

                    customers.LastOrDefault()?.Print();
                    w.Stop();
                    $"Total rows read: {customers.Count}, Elapsed: {w.ElapsedMilliseconds} ms, {w.Elapsed}".Print();
                }
            }
        }

        public class Trade
        {
            public string Id { get; set; }
            public double Price { get; set; }
            public double Quantity { get; set; }
        }

        static void CSV2JSON()
        {
            string filePath = @"C:\Projects\GitHub\ChoETL\data\XBTUSD.csv";
            ChoCSVLiteReader parser = new ChoCSVLiteReader();

            using (var w = new ChoJSONWriter<Trade>(@"C:\Projects\GitHub\ChoETL\data\XBTUSD.json")
                .NotifyAfter(100000)
                .Setup(s => s.RowsWritten += (o, e) =>
                {
                    $"Rows written: {e.RowsWritten}.".Print();
                    if (e.RowsWritten == 1000000)
                        e.Abort = true;
                })
                )
            {
                w.Write(parser.ReadFile<Trade>(filePath, mapper: (lineNo, cols, rec) =>
                {
                    rec.Id = cols[0];
                    rec.Price = cols[1].CastTo<double>();
                    rec.Quantity = cols[2].CastTo<double>();
                }));
            }
        }

        static void ToDataTableFromDictionary()
        {
            var data = TestClassGenerator.GetTestEnumerable2(100000).Select(e => e.ToSimpleDictionary());

            for (int i = 0; i < 10; i++)
            {
                Stopwatch w = Stopwatch.StartNew();
                var dt = data.AsDataTable();
                //dt.Print();
                //break;
                w.Stop();
                w.ElapsedMilliseconds.ToString().Print();
            }
        }

        static void ToDataTableFromNullableValueType()
        {
            List<int?> list = new List<int?>
            {
                1,
                null,
                2
            };

            var dt = list.AsDataTable();
            dt.Print();
        }

        static void ToDataTableFromValueType()
        {
            List<string> list = new List<string>
            {
                "Tom",
                "Mark",
            };

            var dt = list.AsDataTable();
            dt.Print();
        }

        static void ToDataTableTest1()
        {
            var data = TestClassGenerator.GetTestEnumerable(100000);

            for (int i = 0; i < 10; i++)
            {
                Stopwatch w = Stopwatch.StartNew();
                var dt = data.AsDataTable();
                //dt.Print();
                //break;
                w.Stop();
                w.ElapsedMilliseconds.ToString().Print();
            }
        }
    }
}
