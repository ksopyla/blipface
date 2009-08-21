<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="BlipFace.WebServices.Default" %>

<%@ Register Assembly="System.Web.DataVisualization, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
    Namespace="System.Web.UI.DataVisualization.Charting" TagPrefix="asp" %>
<%@ Register Assembly="obout_Calendar2_Net" Namespace="OboutInc.Calendar2" TagPrefix="obout" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Statyski BlipFace</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Początek okresu:
        <asp:TextBox ID="StartTimeTextBox" runat="server" Enabled="False" Width="80px"></asp:TextBox>
        <obout:Calendar ID="StartCalendar" runat="server" DatePickerMode="True" TextBoxId="StartTimeTextBox"
            TitleText="Wybierz datę" CultureName="pl-PL" DatePickerImagePath="~/Img/dateselect.gif"
            DatePickerImageTooltip="Wybierz datę" AutoPostBack="True">
        </obout:Calendar>
        Koniec okresu:
        <asp:TextBox ID="EndTimeTextBox" runat="server" Enabled="False" Width="80px"></asp:TextBox>
        <obout:Calendar ID="EndCalendar" runat="server" DatePickerMode="True" TextBoxId="EndTimeTextBox"
            TitleText="Wybierz datę" CultureName="pl-PL" DatePickerImagePath="~/Img/dateselect.gif"
            DatePickerImageTooltip="Wybierz datę" AutoPostBack="True">
        </obout:Calendar>
        <br />
        Ilość unikalnych użytkowników:<asp:Label ID="UniqUsersLabel" runat="server"></asp:Label>
        <br />
        <asp:Chart ID="UsersUseBlipFaceChart" runat="server" Height="500px" Width="600px">
            <Series>
                <asp:Series Name="Series1">
                </asp:Series>
            </Series>
            <ChartAreas>
                <asp:ChartArea Name="ChartArea1">
                </asp:ChartArea>
            </ChartAreas>
        </asp:Chart>
        <br />
        <asp:Chart ID="UsesVersionBlipFaceChart" runat="server" Height="500px" Width="600px">
            <Series>
                <asp:Series Name="Series1">
                </asp:Series>
            </Series>
            <ChartAreas>
                <asp:ChartArea Name="ChartArea1">
                </asp:ChartArea>
            </ChartAreas>
        </asp:Chart>
    </div>
    </form>
</body>
</html>
