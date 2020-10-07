using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMS_Preprocessing
{
    class Sms_Handler
    {
        private int User_Id { get; set; }

        /// <summary>
        /// Отправит приглашение
        /// </summary>
        /// <param name="phone_numbers">список мобильных номеров незарегистрированных пользователей.</param>
        /// <param name="message">текст сообщения со ссылкой.</param>
        public void SendInvites(string[] phone_numbers, string message)
        {

            if (Encoding.ASCII.GetBytes(message.ToString().ToCharArray()).Length > 160)
                // || isGSM > 128) // стороння библиотека, можно подключить
                throw new Exception("407 BAD_REQUEST MESSAGE_INVALID: Invite message too long, should be less or equal to 128 characters of 7 - bit GSM charset.");

            //if (message.All(x => x.isGSM))
            //    throw new Exception("406 BAD_REQUEST MESSAGE_INVALID: Invite message should contain only characters in 7 - bit GSM encoding or Cyrillic letters as well.");

            if (message.Length < 1) throw new Exception("405 BAD_REQUEST MESSAGE_EMPTY: Invite message is missing.");

            if (phone_numbers.Length < 1) throw new Exception("401 BAD_REQUEST PHONE_NUMBERS_EMPTY: Phone_numbers is missing.");

            if (phone_numbers.Length > 16) throw new Exception("402 BAD_REQUEST PHONE_NUMBERS_INVALID: Too much phone numbers, should be less or equal to 16 per request.");

            if (GetCountInvitations(1) >= 128) throw new Exception("403 BAD_REQUEST PHONE_NUMBERS_INVALID: Too much phone numbers, should be less or equal to 128 per day.");

            if (phone_numbers.GroupBy(x => x).Any(g => g.Count() > 1)) throw new Exception("404 BAD_REQUEST PHONE_NUMBERS_INVALID: Duplicate numbers detected.");

            Parallel.ForEach(phone_numbers.Where(number => number.Contains("+") || number.Contains(" ") || number.Contains("(") || number.Contains(")") || number.Length > 11 || number[0] != '7').Select(number => new { }), (_) =>
            {
                throw new Exception("400 BAD_REQUEST PHONE_NUMBERS_INVALID: One or several phone numbers do not match with international format");
            });


            try
            {
                Parallel.ForEach(phone_numbers, (phone) =>
                {
                    // Предположим тут отправка с помощью какого-нибудь API
                    // Sent(phone, message);
                });
                Invite(User_Id, phone_numbers);
            }
            catch (Exception ex)
            {
                throw new Exception($"500 INTERNAL SMS_SERVICE: {ex.Message}");
            }



        }

        /// <summary>
        /// фиксирует факт отправки приглашений.
        /// </summary>
        /// <param name="integer">id пользователя, автора записи (всегда равен 7);</param>
        /// <param name="text">массив номеров приглашаемых пользователей.</param>
        public void Invite(int user_id, string[] phones)
        {
            string conectingString = "helloServer"; // вставить нужную =)
            using (SqlConnection sqlConnection = new SqlConnection(conectingString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(null, sqlConnection))
                {
                    try
                    {
                        //SqlParameter sqlParameter;
                        sqlCommand.Parameters.Add(new SqlParameter("@user_id", System.Data.SqlDbType.Char, 7));
                        sqlCommand.Parameters.Add(new SqlParameter("@user_phone", System.Data.SqlDbType.Char, 11));
                        sqlCommand.Parameters.Add(new SqlParameter("@Date", System.Data.SqlDbType.DateTime));
                        sqlConnection.Open();

                        sqlCommand.CommandText = "insert into inviteUser (user_id, user_phone, Date) values(@user_id, @user_phone, @Date)";
                        sqlCommand.CommandTimeout = 0;

                        Parallel.ForEach(phones, (phone) =>
                        {
                            sqlCommand.Parameters["user_id"].Value = user_id;
                            sqlCommand.Parameters["user_phone"].Value = phone;
                            sqlCommand.Parameters["Date"].Value = DateTime.Now;
                            sqlCommand.ExecuteNonQuery();
                        });

                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// получает id приложения 
        /// </summary>
        /// <param name="apiid">id приложения (всегда равно 4).</param>
        /// <returns>количество уже отправленных приглашений в течение дня и в рамках определённого приложения.</returns>
        public int GetCountInvitations(int apiid)
        {
            // Предположим что наше app имеет id == 1
            if (apiid == 1)
            {
                string conectingString = "helloServer"; // вставить нужную =)
                using (SqlConnection sqlConnection = new SqlConnection(conectingString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand("select count(user) from inviteUser where Date >= @Date and Date < @Date1"))
                    {
                        try
                        {
                            SqlParameter sqlParameter;
                            sqlParameter = sqlCommand.Parameters.Add(new SqlParameter("@Date", System.Data.SqlDbType.DateTime)); sqlParameter.Value = DateTime.Now;
                            sqlParameter = sqlCommand.Parameters.Add(new SqlParameter("@Date1", System.Data.SqlDbType.DateTime)); sqlParameter.Value = DateTime.Now.AddDays(1);

                            sqlConnection.Open();

                            using (SqlDataReader reader = sqlCommand.ExecuteReader())
                            {
                                if (!reader.IsDBNull(0)) return reader.GetInt32(0);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                }
            }

            return -1;
        }

    }

    class Program
    {

        static void Main(string[] args)
        {
            Sms_Handler sms_Handler = new Sms_Handler();
            sms_Handler.SendInvites(new string[] { "71234567897", "71234567898" }, "HelloWorld!");
        }
    }
}
