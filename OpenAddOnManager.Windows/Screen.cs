using System;
using System.Linq;
using System.Windows;

namespace OpenAddOnManager.Windows
{
    public class Screen
    {
        public static Screen FromFrameworkElement(FrameworkElement frameworkElement)
        {
            var frameworkElementPosition = frameworkElement.PointToScreen(new Point(0, 0));
            var formsScreen = System.Windows.Forms.Screen.FromRectangle(GetDeviceDependentRectangle(new Rect(frameworkElementPosition.X, frameworkElementPosition.Y, frameworkElement.ActualWidth, frameworkElement.ActualHeight)));
            if (formsScreen != null)
                return new Screen(formsScreen);
            return null;
        }

        public static Screen FromHandle(IntPtr hwnd)
        {
            var formsScreen = System.Windows.Forms.Screen.FromHandle(hwnd);
            if (formsScreen != null)
                return new Screen(formsScreen);
            return null;
        }

        public static Screen FromPoint(Point point)
        {
            var formsScreen = System.Windows.Forms.Screen.FromPoint(GetDeviceDependentPoint(point));
            if (formsScreen != null)
                return new Screen(formsScreen);
            return null;
        }

        public static Screen FromRect(Rect rect)
        {
            var formsScreen = System.Windows.Forms.Screen.FromRectangle(GetDeviceDependentRectangle(rect));
            if (formsScreen != null)
                return new Screen(formsScreen);
            return null;
        }

        public static Rect GetBounds(Point point)
        {
            var formsRectangle = System.Windows.Forms.Screen.GetBounds(GetDeviceDependentPoint(point));
            if (formsRectangle != null)
                return GetDeviceIndependentRect(formsRectangle);
            return Rect.Empty;
        }

        public static Rect GetBounds(Rect rect)
        {
            var formsRectangle = System.Windows.Forms.Screen.GetBounds(GetDeviceDependentRectangle(rect));
            if (formsRectangle != null)
                return GetDeviceIndependentRect(formsRectangle);
            return Rect.Empty;
        }

        public static Rect GetBounds(FrameworkElement frameworkElement)
        {
            var frameworkElementPosition = frameworkElement.PointToScreen(new Point(0, 0));
            var formsRectangle = System.Windows.Forms.Screen.GetBounds(GetDeviceDependentRectangle(new Rect(frameworkElementPosition.X, frameworkElementPosition.Y, frameworkElement.ActualWidth, frameworkElement.ActualHeight)));
            if (formsRectangle != null)
                return GetDeviceIndependentRect(formsRectangle);
            return Rect.Empty;
        }

        static System.Drawing.Point GetDeviceDependentPoint(Point point) => new System.Drawing.Point((int)(point.X / XRatio), (int)(point.Y / YRatio));

        static System.Drawing.Rectangle GetDeviceDependentRectangle(Rect rect)
        {
            var xRatio = XRatio;
            var yRatio = YRatio;
            return new System.Drawing.Rectangle((int)(rect.X / xRatio), (int)(rect.Y / yRatio), (int)(rect.Width / xRatio), (int)(rect.Height / yRatio));
        }

        static Rect GetDeviceIndependentRect(System.Drawing.Rectangle formsRectangle)
        {
            var xRatio = XRatio;
            var yRatio = YRatio;
            return new Rect(formsRectangle.Left * xRatio, formsRectangle.Top * yRatio, formsRectangle.Width * xRatio, formsRectangle.Height * yRatio);
        }

        public static Rect GetWorkingArea(Point point)
        {
            var formsRectangle = System.Windows.Forms.Screen.GetWorkingArea(GetDeviceDependentPoint(point));
            if (formsRectangle != null)
                return GetDeviceIndependentRect(formsRectangle);
            return Rect.Empty;
        }

        public static Rect GetWorkingArea(Rect rect)
        {
            var formsRectangle = System.Windows.Forms.Screen.GetWorkingArea(GetDeviceDependentRectangle(rect));
            if (formsRectangle != null)
                return GetDeviceIndependentRect(formsRectangle);
            return Rect.Empty;
        }

        public static Rect GetWorkingArea(FrameworkElement frameworkElement)
        {
            Point frameworkElementPosition;
            try
            {
                frameworkElementPosition = frameworkElement.PointToScreen(new Point(0, 0));
            }
            catch (InvalidOperationException)
            {
                return Rect.Empty;
            }
            catch (NullReferenceException)
            {
                return Rect.Empty;
            }
            var formsRectangle = System.Windows.Forms.Screen.GetWorkingArea(new System.Drawing.Rectangle((int)frameworkElementPosition.X, (int)frameworkElementPosition.Y, (int)(frameworkElement.ActualWidth / XRatio), (int)(frameworkElement.ActualHeight / YRatio)));
            if (formsRectangle != null)
                return GetDeviceIndependentRect(formsRectangle);
            return Rect.Empty;
        }

        public static Screen[] AllScreens => System.Windows.Forms.Screen.AllScreens.Select(s => new Screen(s)).ToArray();

        public static Screen PrimaryScreen => new Screen(System.Windows.Forms.Screen.PrimaryScreen);

        public static double XRatio => SystemParameters.PrimaryScreenWidth / System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;

        public static double YRatio => SystemParameters.PrimaryScreenHeight / System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

        Screen(System.Windows.Forms.Screen formsScreen)
        {
            this.formsScreen = formsScreen;
            _ = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
        }

        readonly System.Windows.Forms.Screen formsScreen;

        public override bool Equals(object obj)
        {
            if (obj is Screen b)
                return formsScreen == b.formsScreen;
            return base.Equals(obj);
        }

        public override int GetHashCode() => formsScreen.GetHashCode();

        public override string ToString()
        {
            var bounds = Bounds;
            var workingArea = WorkingArea;
            return $"S[B[X{bounds.X}Y{bounds.Y}W{bounds.Width}H{bounds.Height}]W[X{workingArea.X}Y{workingArea.Y}W{workingArea.Width}H{workingArea.Height}]]";
        }

        public int BitsPerPixel => formsScreen.BitsPerPixel;

        public Rect Bounds => GetDeviceIndependentRect(formsScreen.Bounds);

        public string DeviceName => formsScreen.DeviceName;

        public bool Primary => formsScreen.Primary;

        public Rect WorkingArea => GetDeviceIndependentRect(formsScreen.WorkingArea);
    }
}
