using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Enterprise.Infrastructure
{
    public static class Extentions
    {
        private static Dictionary<int, string> _month;

        public static Dictionary<int, string> Months
        {
            get
            {
                if (_month == null)
                {
                    _month = new Dictionary<int, string>
                    {
                        {1, "січня"},
                        {2, "лютого"},
                        {3, "березня"},
                        {4, "квітня"},
                        {5, "травня"},
                        {6, "червня"},
                        {7, "липня"},
                        {8, "серпня"},
                        {9, "вересня"},
                        {10, "жовтня"},
                        {11, "листопада"},
                        {12, "грудня"}
                    };
                }
                return _month;
            }
        }

        public static string ToUkrDateTimeString(this DateTime dateTime)
        {
            return string.Format("{0} {1} {2}р. {3:D2}:{4:D2}", dateTime.Day, Months[dateTime.Month], dateTime.Year,
                dateTime.Hour, dateTime.Minute);
        }

        #region Chart extension

        public static HtmlString Chart(this HtmlHelper helper,
            string chartName,
            List<double[]> dataSource,
            string xTitle,
            string yTitle,
            double maxValue)
        {
            return SetupHtml(chartName, GetDataSourceFromIntArray(dataSource),
                xTitle, yTitle, maxValue);
        }

        private static string GetDataSourceFromIntArray(List<double[]> dataSource)
        {
            return "arrDataSource = " + Json.Encode(dataSource);
        }

        private static HtmlString SetupHtml(string chartName, string dataSource,
            string xTitle, string yTitle, double maxValue)
        {
            TagBuilder container = new TagBuilder("center");
            container.Attributes.Add("style", "border: solid 1px #F0F0F0;");

            TagBuilder canvas = new TagBuilder("canvas");
            canvas.Attributes.Add("id", chartName);
            canvas.Attributes.Add("height", "500");
            canvas.Attributes.Add("width", "1000");
            canvas.SetInnerText(@"Your browser does not support HTML5 Canvas");

            TagBuilder script = SetupScript(chartName, dataSource, xTitle, yTitle, maxValue);

            TagBuilder noScript = new TagBuilder("noscript");
            noScript.InnerHtml = @"
                This chart is unavailable because JavaScript is disabled on your computer. 
                Please enable JavaScript and refresh this page to see the chart in action.";
            container.InnerHtml = canvas.ToString() +
                                  script.ToString() +
                                  noScript.ToString();
            return new HtmlString(container.ToString());
        }

        private static TagBuilder SetupScript(string chartName, string dataSource,
            string xTitle, string yTitle, double maxValue)
        {
            TagBuilder script = new TagBuilder("script");
            script.Attributes.Add("type", "text/javascript");

            script.InnerHtml = @"
    <!--
        // chart sample data
        var arrDataSource = new Array();
        " + dataSource + @"
        var canvas;
        var context;
        // chart properties
        var cWidth, cHeight, cMargin, cSpace;
        var cMarginSpace, cMarginHeight;
        // bar properties
        var bWidth, bMargin, totalBars, maxDataValue;
        var bWidthMargin;
        // bar animation
        var ctr, numctr, speed;
        // axis property
        var totLabelsOnYAxis;


        // barchart constructor
        function barChart(data) {
            if(data!=null)
            {
                arrDataSource = data;
            }
            canvas = document.getElementById('" + chartName + @"');
            if (canvas && canvas.getContext) {
                context = canvas.getContext('2d');
            }

            chartSettings();
            drawAxisLabelMarkers();
            drawChartWithAnimation();
        }

        // initialize the chart and bar values
        function chartSettings() {
            // chart properties
            cMargin = 25;
            cSpace = 60;
            cHeight = canvas.height - 2 * cMargin - cSpace;
            cWidth = canvas.width - 2 * cMargin - cSpace;
            cMarginSpace = cMargin + cSpace;
            cMarginHeight = cMargin + cHeight;
            // bar properties
            bMargin = 15;
            totalBars = arrDataSource.length;
            bWidth = (cWidth / (totalBars)) - bMargin;


            // find maximum value to plot on chart
            maxDataValue = parseFloat('" + maxValue.ToString() + @"');
            //for (var i = 0; i < totalBars; i++) {
            //    var barVal = parseFloat(arrDataSource[i][1]);
            //    if (parseFloat(barVal) > parseFloat(maxDataValue))
            //        maxDataValue = barVal;
            //}

            totLabelsOnYAxis = 10;
            context.font = '10pt Garamond';

            // initialize Animation variables
            ctr = 0;
            numctr = 100;
            speed = 10;
        }

        // draw chart axis, labels and markers
        function drawAxisLabelMarkers() {
            context.lineWidth = '2.0';
            // draw y axis
            drawAxis(cMarginSpace, cMarginHeight, cMarginSpace, cMargin);
            // draw x axis
            drawAxis(cMarginSpace, cMarginHeight, cMarginSpace + cWidth + 10, cMarginHeight);
            context.lineWidth = '1.0';
            drawMarkers();
        }

        // draw X and Y axis
        function drawAxis(x, y, X, Y) {
            context.beginPath();
            context.moveTo(x, y);
            context.lineTo(X, Y);
            context.closePath();
            context.stroke();
        }

        // draw chart markers on X and Y Axis
        function drawMarkers() {
            var numMarkers = parseFloat(maxDataValue / totLabelsOnYAxis);
            context.textAlign = 'right';
            context.fillStyle = '#000'; ;

            // Y Axis
            for (var i = 0; i <= totLabelsOnYAxis; i++) {
                markerVal = i * numMarkers;
                markerValHt = i * numMarkers * cHeight;
                var xMarkers = cMarginSpace - 5;
                var yMarkers = cMarginHeight - (markerValHt / maxDataValue);
                context.fillText(Math.round(markerVal * 100)/100, xMarkers, yMarkers, cSpace);
            }

            // X Axis
            context.textAlign = 'center';
            var lablesNum = Math.min(totalBars, 10);
            var lWidth = (cWidth / (lablesNum)) - bMargin;
            for (var i = 0; i < lablesNum; i++) {
                //arrval = arrDataSource[i].split(',');
                //name = arrval[0];
                name = arrDataSource[Math.min(Math.max(0, ((i+1)*parseInt(arrDataSource.length/lablesNum) - 1)),arrDataSource.length-1)][0];

                markerXPos = cMarginSpace + bMargin + (i * (lWidth + bMargin)) + (lWidth / 2);
                markerYPos = cMarginHeight + 10;
                context.fillText(name, markerXPos, markerYPos, lWidth);
            }

            context.save();

            // Add Y Axis title
            context.translate(cMargin + 10, cHeight / 2);
            context.rotate(Math.PI * -90 / 180);
            context.fillText('" + yTitle + @"', 0, 0);

            context.restore();

            // Add X Axis Title
            context.fillText('" + xTitle + @"', cMarginSpace + (cWidth / 2), cMarginHeight + 30);
        }

        function drawChartWithAnimation() {
            // Loop through the total bars and draw
            for (var i = 0; i < totalBars; i++) {
                //var arrVal = arrDataSource[i].split(',');
                //bVal = parseFloat(arrVal[1]);
                bVal = parseFloat(arrDataSource[i][1]);
                bHt = (bVal * cHeight / maxDataValue) / numctr * ctr;
                bX = cMarginSpace + (i * (bWidth + bMargin)) + bMargin;
                bY = cMarginHeight - bHt - 2;
                drawRectangle(bX, bY, bWidth, bHt, true);
            }

            // timeout runs and checks if bars have reached the desired height
            // if not, keep growing
            if (ctr < numctr) {
                ctr = ctr + 1;
                setTimeout(arguments.callee, speed);
            }
        }

        function drawRectangle(x, y, w, h, fill) {
            context.beginPath();
            context.rect(x, y, w, h);
            context.closePath();
            context.stroke();

            if (fill) {
                var gradient = context.createLinearGradient(0, 0, 0, 300);
                gradient.addColorStop(0, 'green');
                gradient.addColorStop(1, 'rgba(67,203,36,.15)');
                context.fillStyle = gradient;
                context.strokeStyle = gradient;
                context.fill();
            }
        }
        -->
    ";
            return script;
        }

        #endregion
    }
}