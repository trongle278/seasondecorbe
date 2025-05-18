using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;

namespace BusinessLogicLayer.Utilities.Extension
{
    public static class BookingFormExtension
    {
        public static bool IsEmpty(this BookingFormRequest form)
        {
            return string.IsNullOrWhiteSpace(form.SpaceStyle)
                && !form.RoomSize.HasValue
                && string.IsNullOrWhiteSpace(form.Style)
                && string.IsNullOrWhiteSpace(form.ThemeColor)
                && string.IsNullOrWhiteSpace(form.PrimaryUser)
                && (form.Images == null || !form.Images.Any());
        }
    }
}
