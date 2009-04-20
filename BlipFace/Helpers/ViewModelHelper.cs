using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlipFace.Service.Entities;
using BlipFace.Model;

namespace BlipFace.Helpers
{
    /// <summary>
    /// Klasa pomocnicza
    /// </summary>
    public class ViewModelHelper
    {

        /// <summary>
        /// Pomocna metoda do mapowania Entities do ViewEntities
        /// </summary>
        /// <param name="iList"></param>
        /// <returns></returns>
        public static IList<StatusViewModel> MapToViewStatus(IList<BlipFace.Service.Entities.BlipStatus> iList)
        {
            IList<StatusViewModel> sts = new List<StatusViewModel>(20);
            foreach (BlipStatus status in iList)
            {
                //todo: trzeba uważać bo gdy nie ma recipient to 
                //rzuca wyjątekiem nullreference
                string reciptientAvatar = string.Empty;
                string reciptientLogin = string.Empty;
                if (status.Type == "PrivateMessage" || status.Type == "DirectedMessage")
                {
                    reciptientAvatar = status.Recipient.Avatar.Url50;
                    reciptientLogin = status.Recipient.Login;
                }

                sts.Add(new StatusViewModel()
                {
                    StatusId = status.Id,
                    UserId = status.User.Id,
                    Content = status.Content,
                    UserAvatar50 = status.User.Avatar.Url50,
                    RecipientAvatar50 = reciptientAvatar,
                    RecipientLogin =reciptientLogin,
                    CreationDate = status.StatusTime,
                    UserLogin = status.User.Login
                });
            }

            return sts;
        }
    }
}
