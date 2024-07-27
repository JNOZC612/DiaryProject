using Microsoft.Data.Sqlite;
using MyDiary.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDiary.db
{
    class DBHelper
    {
        private SqliteConnection connection;
        public DBHelper(string dbPath)
        {
            connection = new SqliteConnection($"Data Source ={dbPath}");
            connection.Open();
        }
        public void CreateDB()
        {
            string tableCommand = "CREATE TABLE Records (Date TEXT PRIMARY KEY, Content TEXT)";
            var createTable = new SqliteCommand(tableCommand, connection);
            createTable.ExecuteNonQuery();
        }
        public void UpsertRecord(Record record)
        {
            string upsertCommand = @"
                INSERT INTO Records (Date, Content)
                VALUES (@Date, @Content)
                ON CONFLICT(Date)
                DO UPDATE SET Content = @Content";
            var upsertData = new SqliteCommand(upsertCommand, connection);
            upsertData.Parameters.AddWithValue("@Date", record.Date);
            upsertData.Parameters.AddWithValue("@Content", record.Content);
            upsertData.ExecuteNonQuery();
        }
        public void DeleteRecord(string date)
        {
            string deleteCommand = "DELETE FROM Records WHERE Date = @Date";
            var deleteData = new SqliteCommand(deleteCommand, connection);
            deleteData.Parameters.AddWithValue("@Date", date);
            deleteData.ExecuteNonQuery();
        }
        public Record GetRecordByDate(string date)
        {
            string selectCommand = "SELECT Date, Content FROM Records WHERE Date = @Date";
            var selectData = new SqliteCommand(selectCommand, connection);
            selectData.Parameters.AddWithValue("@Date", date);
            selectData.ExecuteNonQuery();
            var reader = selectData.ExecuteReader();
            if (reader.Read())
            {
                var record = new Record
                {
                    Date = reader.GetString(0),
                    Content = reader.GetString(1)
                };
                reader.Close();
                reader.Dispose();
                return record;
            }
            reader.Close();
            reader.Dispose();
            return null;
        }
        public List<Record> GetRecords()
        {
            var records = new List<Record>();
            string selectCommand = "SELECT Date, Content FROM Records";
            var selectData = new SqliteCommand(selectCommand, connection);
            var reader = selectData.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new Record
                {
                    Date = reader.GetString(0),
                    Content = reader.GetString(1)
                });
            }
            reader.Close();
            reader.Dispose();
            return records;
        }
        public void DispatchDB()
        {
            connection.Close();
            connection.Dispose();
            SqliteConnection.ClearAllPools();
        }
    }
}
