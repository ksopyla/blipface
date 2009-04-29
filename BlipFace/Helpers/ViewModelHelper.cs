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
            IList<StatusViewModel> sts = new List<StatusViewModel>(iList.Count);
            try
            {
                foreach (BlipStatus status in iList)
                {
                    //todo: trzeba uważać bo gdy nie ma recipient to 
                    //rzuca wyjątekiem nullreference
                    string reciptientAvatar = string.Empty;
                    string reciptientLogin = string.Empty;
                    if (status.Type == "PrivateMessage" || status.Type == "DirectedMessage")
                    {
                        reciptientAvatar = status.Recipient.Avatar== null
                                           ? "http://static1.blip.pl/images/nn_nano.png?1240395130"
                                           : status.Recipient.Avatar.Url50;
                        reciptientLogin = status.Recipient.Login;
                    }

                    ///czasami data nie jest ustawiana przez Blipa - dziwne
                    string creationDate = status.StatusTime == null ? string.Empty : status.StatusTime;
                    string avatarUrl = status.User.Avatar == null
                                           ? "http://static1.blip.pl/images/nn_nano.png?1240395130"
                                           : status.User.Avatar.Url50;

                    sts.Add(new StatusViewModel()
                                {
                                    StatusId = status.Id,
                                    UserId = status.User.Id,
                                    Content = status.Content,
                                    UserAvatar50 = avatarUrl,
                                    RecipientAvatar50 = reciptientAvatar,
                                    RecipientLogin = reciptientLogin,
                                    CreationDate = creationDate,
                                    UserLogin = status.User.Login
                                });
                }
            }
            catch (Exception e)
            {
                throw;
            }


            return sts;
        }

        /// <summary>
        /// Pomocna metoda do mapowania Entities do ViewEntities
        /// </summary>
        /// <param name="iList"></param>
        /// <returns></returns>
        public static StatusViewModel MapToViewStatus(BlipFace.Service.Entities.BlipStatus status)
        {
            StatusViewModel st = new StatusViewModel();
            try
            {
                //todo: trzeba uważać bo gdy nie ma recipient to 
                //rzuca wyjątekiem nullreference
                string reciptientAvatar = string.Empty;
                string reciptientLogin = string.Empty;
                
                //czasami data nie jest ustawiana przez Blipa - dziwne
                string creationDate = string.Empty;
                string avatarUrl = status.User.Avatar == null
                                       ? "http://static1.blip.pl/images/nn_nano.png?1240395130"
                                       : status.User.Avatar.Url50;

                st.StatusId = status.Id;
                st.UserId = status.User.Id;
                st.Content = status.Content;
                st.UserAvatar50 = avatarUrl;
                st.RecipientAvatar50 = reciptientAvatar;
                st.RecipientLogin = reciptientLogin;
                st.CreationDate = creationDate;
                st.UserLogin = status.User.Login;
            }
            catch (Exception e)
            {
                throw;
            }


            return st;
        }
    }
}