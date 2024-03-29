using System;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using ShinobiCharts;
using System.Net;
using System.Linq;

namespace ShinobiStockChart
{
	public partial class StockChartViewController : UIViewController
	{
		private ShinobiChart _chart;
		private StockChartDataSource _charDataSource = new StockChartDataSource();
		private string _symbol;

		public StockChartViewController (string symbol) : base ("StockChartViewController", null)
		{
			_symbol = symbol;
			FechPriceData ();
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			symbolLabel.Text = _symbol;

			// create the chart and add to the view
			_chart = new ShinobiChart (chartHostView.Bounds)
				{
					LicenseKey = @"ft6obh5sR2HPP7gMjAxMjExMjdpbmZvQHNoaW5vYmljb250cm9scy5jb20=DoMioOfF4W6zCl90EggbYPCPBNUFz7KZI8jJaqH4TopBrv+A72R/DiFUWilWUIZIzd3Fq/c4smVsPQUtz+g6WZ2fEbW5C8/zAR/vNbl9h4dhwozVXjmet42hJJPu+qgPqbX1XSnQfIxpqO7wI+B4DgfMsrP8=BQxSUisl3BaWf/7myRmmlIjRnMU2cA7q+/03ZX9wdj30RzapYANf51ee3Pi8m2rVW6aD7t6Hi4Qy5vv9xpaQYXF5T7XzsafhzS3hbBokp36BoJZg8IrceBj742nQajYyV7trx5GIw9jy/V6r0bvctKYwTim7Kzq+YPWGMtqtQoU=PFJTQUtleVZhbHVlPjxNb2R1bHVzPnh6YlRrc2dYWWJvQUh5VGR6dkNzQXUrUVAxQnM5b2VrZUxxZVdacnRFbUx3OHZlWStBK3pteXg4NGpJbFkzT2hGdlNYbHZDSjlKVGZQTTF4S2ZweWZBVXBGeXgxRnVBMThOcDNETUxXR1JJbTJ6WXA3a1YyMEdYZGU3RnJyTHZjdGhIbW1BZ21PTTdwMFBsNWlSKzNVMDg5M1N4b2hCZlJ5RHdEeE9vdDNlMD08L01vZHVsdXM+PEV4cG9uZW50PkFRQUI8L0V4cG9uZW50PjwvUlNBS2V5VmFsdWU+",
				};

			View.BackgroundColor = _chart.Theme.ChartStyle.BackgroundColor;
			_chart.Theme = new SChartMidnightTheme();
			_chart.DataSource = _charDataSource;
			
			// add a couple of axes
			_chart.XAxis = new SChartDateTimeAxis ();
			_chart.YAxis = new SChartNumberAxis ();
			ConfigureAxis (_chart.XAxis);
			ConfigureAxis (_chart.YAxis);

			// add a fancy border to the loading indicato
			progressIndicatorView.Layer.MasksToBounds = false;
			progressIndicatorView.Layer.CornerRadius = 10;
			progressIndicatorView.Layer.ShadowColor = UIColor.DarkGray.CGColor;
			progressIndicatorView.Layer.ShadowOpacity = 1.0f;
			progressIndicatorView.Layer.ShadowRadius = 6.0f;
			progressIndicatorView.Layer.ShadowOffset = new SizeF (0f, 3f);

			chartHostView.Hidden = true;
			chartHostView.InsertSubview (_chart, 0);
		}

		private void ConfigureAxis (SChartAxis axis)
		{
			axis.EnableGesturePanning = true;
			axis.EnableGestureZooming = true;
			axis.EnableMomentumPanning = true;
			axis.EnableMomentumZooming = true;
		}
	
		private void FechPriceData ()
		{
			var url = string.Format ("http://ichart.finance.yahoo.com/table.csv?d=0&e=28&f=2013&g=d&a=3&b=12&c=1996&ignore=.csv&s={0}", _symbol);

			var client = new WebClient ();
			client.DownloadStringCompleted += (s,e) => { ParseCSVStockPrices (e.Result); };
			client.DownloadStringAsync (new Uri (url));
		}

		private void ParseCSVStockPrices (string csvData)
		{
			var seriesData = new List<SChartData> ();

			var lines = csvData.Split ('\n');
			foreach (var line in lines.Skip(1)) {
				var components = line.Split (',');
				if (components.Length > 1)
				{
					var date = DateTime.Parse (components [0]);
					var value = double.Parse (components [1]);
					seriesData.Add (
						new SChartDataPoint () {
							XValue = date.ToNSDate(),
							YValue = new NSNumber(value)
						}
					);
				}
			}

			InvokeOnMainThread (() => {
				_charDataSource.DataPoints = seriesData;
				_chart.ReloadData ();
				_chart.RedrawChart ();
				progressIndicatorView.Hidden = true;
				chartHostView.Hidden = false;
			});
		}

		private class StockChartDataSource : SChartDataSource
		{
			private List<SChartData> _dataPoints;

			public StockChartDataSource ()
			{
				_dataPoints = new List<SChartData> ();
			}

			public List<SChartData> DataPoints {
				set { _dataPoints = value; }
			}

			public override SChartData GetDataPoint (ShinobiChart chart, int dataIndex, int seriesIndex)
			{
				// no-op
				return null;
			}

			protected override SChartData[] GetDataPoints (ShinobiChart chart, int seriesIndex)
			{
				return _dataPoints.ToArray ();
			}

			public override int GetNumberOfSeries (ShinobiChart chart)
			{
				return 1;
			}

			public override int GetNumberOfDataPoints (ShinobiChart chart, int seriesIndex)
			{
				return _dataPoints.Count;
			}

			public override SChartSeries GetSeries (ShinobiChart chart, int index)
			{
				var lineSeries = new SChartLineSeries ();

				lineSeries.Style.LineColor = UIColor.FromRGB(166,166,166);
				lineSeries.Style.AreaColor = UIColor.FromRGB (16, 99, 123);
				lineSeries.Style.AreaColorLowGradient = UIColor.FromRGB (0, 0, 41);
				lineSeries.Style.ShowFill = true;

				lineSeries.CrosshairEnabled = true;

				return lineSeries;
			}
		}
	}
}