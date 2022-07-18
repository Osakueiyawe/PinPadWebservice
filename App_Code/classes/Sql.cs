using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Summary description for Sql
/// </summary>
internal class Sql :IDisposable
{
    private bool dispose = false;
    public Sql()
    {

    }
    public int ExecuteSqlNonQuery(string query, string connectionString, bool isProcedure, params SqlParameter[] sqlParam)
    {
        int result = 0;
        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand comm = new SqlCommand(query, con);
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }

                foreach (SqlParameter item in sqlParam)
                {
                    comm.Parameters.Add(item);
                }
                
                comm.CommandType = isProcedure ? CommandType.StoredProcedure: CommandType.Text;

                result = comm.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);               
        }

        return result;
    }

    public string ExecuteSqlScalar(string query, string connectionString, bool isProcedure, params SqlParameter[] sqlParam)
    {
        string result = "ERROR";
        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand comm = new SqlCommand(query, con);
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }

                foreach (SqlParameter item in sqlParam)
                {
                    comm.Parameters.Add(item);
                }

                comm.CommandType = isProcedure ? CommandType.StoredProcedure:CommandType.Text  ;

               object res = comm.ExecuteScalar();
               result = (res == null) ? (string)res : res.ToString();
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);               
        }
        return result;
    }

    public DataSet Execute(string query, string connectionString, bool isProcedure, params SqlParameter[] sqlParam)
    {
        DataSet ds = new DataSet();

        try
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand comm = new SqlCommand(query, con);
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }

                foreach (SqlParameter item in sqlParam)
                {
                    comm.Parameters.Add(item);
                }

                comm.CommandType = isProcedure ? CommandType.StoredProcedure :CommandType.Text ;

                SqlDataReader reader = comm.ExecuteReader();
                SqlDataAdapter adp = new SqlDataAdapter(comm.CommandText, con);
                adp.Fill(ds);
                return ds;
            }
        }
        catch (Exception ex)
        {
            Utility.Log().Fatal("Database Call Failed ", ex);               
        }


        return new DataSet();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (this != null)
                Dispose();
        }
    }  
}