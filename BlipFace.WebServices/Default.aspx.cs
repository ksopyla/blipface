using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.DataVisualization.Charting;

namespace BlipFace.WebServices
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                StartCalendar.SelectedDate = DateTime.Now.AddDays(-10);
                StartCalendar.DataBind();
                EndCalendar.SelectedDate = DateTime.Now;
                EndCalendar.DataBind();
            }

            using (DataClassesDataContext db = new DataClassesDataContext())
            {
                #region UsersUseBlipFaceInDay
                var UsersUseBlipFaceInDay = from usesBlipFace in db.CountUses
                                            where usesBlipFace.DateUse.Date.CompareTo(StartCalendar.SelectedDate.Date) >= 0 && usesBlipFace.DateUse.Date.CompareTo(EndCalendar.SelectedDate.Date) <= 0
                                            group usesBlipFace by usesBlipFace.DateUse.Date into useInDay
                                            select new { Date = useInDay.Key, CountUse = useInDay.ToList().GroupBy(a => a.UserGuid).Count() };

                // Set series chart type
                UsersUseBlipFaceChart.Series["Series1"].ChartType = SeriesChartType.Bar;

                // Set series point width
                UsersUseBlipFaceChart.Series["Series1"]["PointWidth"] = "0.8";

                // Show data points labels
                UsersUseBlipFaceChart.Series["Series1"].IsValueShownAsLabel = true;

                // Set data points label style
                UsersUseBlipFaceChart.Series["Series1"]["BarLabelStyle"] = "Center";

                // Draw as 3D Cylinder
                UsersUseBlipFaceChart.Series["Series1"]["DrawingStyle"] = "Cylinder";

                UsersUseBlipFaceChart.DataSource = UsersUseBlipFaceInDay.ToList(); ;
                UsersUseBlipFaceChart.Series["Series1"].XValueMember = "Date";
                UsersUseBlipFaceChart.Series["Series1"].YValueMembers = "CountUse";
                UsersUseBlipFaceChart.Titles.Add("Ilość użytkowników BlipFace w poszczególnych dniach");

                UsersUseBlipFaceChart.DataBind();
                #endregion

                #region UniqUserUsesBlipFace

                var uniqUserUserBlipFace = from userGuid in db.CountUses
                                           where userGuid.DateUse.Date.CompareTo(StartCalendar.SelectedDate.Date) >= 0 && userGuid.DateUse.Date.CompareTo(EndCalendar.SelectedDate.Date) <= 0
                                           group userGuid by userGuid.UserGuid into uniqUser
                                           select uniqUser;

                UniqUsersLabel.Text = uniqUserUserBlipFace.Count().ToString();

                #endregion

                #region UsesVersionBlipFace
                var usesVersionBlipFace = from usesBlipFace in db.CountUses
                                            where usesBlipFace.DateUse.Date.CompareTo(StartCalendar.SelectedDate.Date) >= 0 && usesBlipFace.DateUse.Date.CompareTo(EndCalendar.SelectedDate.Date) <= 0
                                            group usesBlipFace by usesBlipFace.Version into useVersion
                                            select new { Date = useVersion.Key, CountUse = useVersion.ToList().GroupBy(a => a.UserGuid).Count() };

                // Set series chart type
                UsesVersionBlipFaceChart.Series["Series1"].ChartType = SeriesChartType.Pie;

                // Set series point width
                UsesVersionBlipFaceChart.Series["Series1"]["PointWidth"] = "0.8";

                // Show data points labels
                UsesVersionBlipFaceChart.Series["Series1"].IsValueShownAsLabel = true;

                // Set data points label style
                UsesVersionBlipFaceChart.Series["Series1"]["BarLabelStyle"] = "Center";

                // Draw as 3D Cylinder
                UsesVersionBlipFaceChart.Series["Series1"]["DrawingStyle"] = "Cylinder";

                UsesVersionBlipFaceChart.DataSource = usesVersionBlipFace.ToList(); ;
                UsesVersionBlipFaceChart.Series["Series1"].XValueMember = "Date";
                UsesVersionBlipFaceChart.Series["Series1"].YValueMembers = "CountUse";
                UsesVersionBlipFaceChart.Titles.Add("Używane wersje BlipFace");

                UsersUseBlipFaceChart.DataBind();
                #endregion
            }
        }
    }
}
