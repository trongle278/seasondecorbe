using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        public int WalletId { get; set; }

        public int? BookingId { get; set; }

        public int? OrderId { get; set; }

        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; }

        // Định nghĩa enum cho loại chuyển khoản
        public enum EnumTransactionType
        {
            TopUp, // Nạp tiền
            Withdraw // rút tiền
        }
        public EnumTransactionType TransactionType { get; set; }

        // Định nghĩa enum trạng thái chuyển khoản
        public enum EnumTransactionStatus
        {
            Pending,    // Đang chờ
            Success,    // Thành công
            Failed      // Thất bại
        }

        public EnumTransactionStatus TransactionStatus { get; set; }

        public virtual Wallet Wallet { get; set; }


        public virtual Booking Booking { get; set; }

        public virtual Order Order { get; set; }
    }
}
