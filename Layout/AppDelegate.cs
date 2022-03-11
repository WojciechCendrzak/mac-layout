using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using AppKit;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;

namespace UISample
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        const string ApplicationServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
        const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
        const int kAXIllegalType = 0;
        const int kAXValueCGPointType = 1;
        const int kAXValueCGSizeType = 2;
        const int kAXValueCGRectType = 3;

        [DllImport(ApplicationServices)]
        static extern bool AXAPIEnabled();

        [DllImport(ApplicationServices)]
        static extern IntPtr AXUIElementCreateApplication(int pid);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXUIElementCopyAttributeValue(IntPtr element, IntPtr attribute, ref IntPtr pointer);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXUIElementCopyAttributeValues(IntPtr element, IntPtr attribute, int index, int MaxValues, ref IntPtr pointer);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXUIElementCopyAttributeNames(IntPtr element, ref IntPtr pointer);

        //[DllImport(ApplicationServices)]
        //static extern IntPtr AXValueCreate(int valueType, ref bool boolValue);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXValueCreate(int valueType, ref CGRect cgRect);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXValueGetValue(IntPtr cfTypePtr, int valueType, ref bool boolValue);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXValueGetValue(IntPtr cfTypePtr, int valueType, ref CGRect cgRect);

        [DllImport(ApplicationServices)]
        static extern int AXUIElementSetAttributeValue(IntPtr element, IntPtr attribute, IntPtr value);
        //AXError AXUIElementSetAttributeValue(AXUIElementRef element, CFStringRef attribute, CFTypeRef value);

        [DllImport(CoreGraphics)]
        static extern IntPtr CFArrayGetValueAtIndex(IntPtr element, int index);

        [DllImport(CoreGraphics)]
        static extern void CFRelease(IntPtr cf);

        [DllImport(Constants.CoreGraphicsLibrary)]
        static extern IntPtr CGWindowListCopyWindowInfo(CGWindowListOption options, uint relativeToWindowId);

        public AppDelegate()
        {
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            if (!AXAPIEnabled())
            {
                Debug.WriteLine("API Disabled.");
                return;
            }

            var appName = "Notes";

            var frame = GetMainFrame(appName);
            Debug.WriteLine($"main {appName} frame: {frame}");

            var newFrame = new CGRect(frame.X + 20, frame.Y, frame.Width, frame.Height);
            Debug.WriteLine($"new frame: {newFrame}");
            setMainFrame(appName, newFrame);

            var frameAfter = GetMainFrame(appName);
            Debug.WriteLine($"main {appName} frame after: {frameAfter}");

            var itWorks = frame.X != frameAfter.X;
            Debug.WriteLine($"itWorks {itWorks}");

            //TODO: dont work

        }

        private CGRect GetMainFrame(string appName)
        {
            var windowPtr = getMainWindow(appName);
            //enumerateAttributes(windowPtr);
            var frame = GetFrameAttribute(windowPtr);
            return frame;
        }

        private void setMainFrame(string appName, CGRect frame)
        {
            var windowPtr = getMainWindow(appName);
            SetFrameAttibute(windowPtr, frame);
        }

        private IntPtr getMainWindow(string appName)
        {
            var app = GetWindowByName(appName);
            if (app == null)
            {
                Debug.WriteLine($"{appName} not found");
                return IntPtr.Zero;

            }
            var pid = app.ValueForKey((NSString)"kCGWindowOwnerPID");
            var pidi = int.Parse(pid.ToString());
            var ax_app = AXUIElementCreateApplication(pidi);

            IntPtr windowsPtr = getAttribute(ax_app, "AXWindows");

            var window = NSArray.ArrayFromHandle<NSObject>(windowsPtr).FirstOrDefault();

            if (window == null)
            {
                Debug.WriteLine($"no main window");
                return IntPtr.Zero;
            }

            var windowPtr = CFArrayGetValueAtIndex(windowsPtr, 0);
            return windowPtr;
        }

        public static NSDictionary[] CopyWindowInfo(CGWindowListOption options, uint relativeToWindowId)
        {
            return NSArray.ArrayFromHandle<NSDictionary>(CGWindowListCopyWindowInfo(options, relativeToWindowId));
        }

        public static NSDictionary GetWindowByName(string name)
        {
            return CopyWindowInfo(CGWindowListOption.OnScreenOnly, 0)
                .Where(w => w.ValueForKey((NSString)"kCGWindowOwnerName").ToString() == name)
                .FirstOrDefault();
        }

        private IntPtr getAttribute(IntPtr parent, string name)
        {
            IntPtr attrPtr = IntPtr.Zero;
            AXUIElementCopyAttributeValues(parent, new CFString(name).Handle, 0, 99999, ref attrPtr);
            return attrPtr;
        }

        private void enumerateChildrens(IntPtr parentAXUIElementRef)
        {
            var kAXChildren = new CFString("AXChildren");
            IntPtr attribValuesPtr = IntPtr.Zero;
            AXUIElementCopyAttributeValues(parentAXUIElementRef, kAXChildren.Handle, 0, 99999, ref attribValuesPtr);
            var children = NSArray.ArrayFromHandle<NSObject>(attribValuesPtr);
            if (children != null)
            {
                int i = 0;
                foreach (var child in children)
                {
                    var axUIElementPtr = CFArrayGetValueAtIndex(attribValuesPtr, i);

                    Debug.WriteLine($"i: {i}");

                    enumerateAttributes(axUIElementPtr);

                    CFRelease(axUIElementPtr);
                    i++;
                }
            }
        }

        private void enumerateAttributes(IntPtr axUIElementPtr)
        {
            IntPtr namesPtr = IntPtr.Zero;
            AXUIElementCopyAttributeNames(axUIElementPtr, ref namesPtr);
            var attributes = NSArray.ArrayFromHandle<NSObject>(namesPtr);

            foreach (var attrib in attributes)
            {
                switch (attrib.Description)
                {
                    case "AXTitle":
                    case "AXIdentifier":
                    case "AXHelp":
                    case "AXRole":
                    case "AXSubRole":
                    case "AXRoleDescription":
                    case "AXValue":
                        Debug.WriteLine(attrib.Description + ":" + GetStringAttribute(axUIElementPtr, attrib.Description));
                        break;
                    case "AXEnabled":
                    case "AXFocused":
                        // I beg you please show me an example to get a boolean attribute.
                        break;
                    case "AXFrame":
                        Debug.WriteLine(attrib.Description + ":" + GetFrameAttribute(axUIElementPtr));
                        break;
                    case "AXPosition":
                        Debug.WriteLine(attrib.Description + ":" + GetFrameAttribute(axUIElementPtr));
                        break;
                    default:
                        Debug.WriteLine(attrib.Description);
                        break;
                }
            }
        }

        private string GetStringAttribute(IntPtr axUIElementPtr, string attributeName)
        {
            return CFString.FromHandle(GetAttributePtr(axUIElementPtr, attributeName));
        }

        private CGRect GetFrameAttribute(IntPtr axUIElementPtr)
        {
            var attribPtr = GetAttributePtr(axUIElementPtr, "AXFrame");
            CGRect rect = new CGRect(0, 0, 0, 0);
            AXValueGetValue(attribPtr, kAXValueCGRectType, ref rect);
            return rect;
        }

        private void SetFrameAttibute(IntPtr axUIElementPtr, CGRect frame)
        {

            Debug.WriteLine($"AXValueCreate frame {frame}");

            var framePtr = AXValueCreate(kAXValueCGRectType, ref frame);
            Debug.WriteLine($"AXValueCreate framePtr {framePtr}");

            CGRect frameTest1 = new CGRect(0, 0, 0, 0);
            AXValueGetValue(framePtr, kAXValueCGRectType, ref frameTest1);
            Debug.WriteLine($"AXValueCreate frameTest1 {frameTest1}");

            var error = AXUIElementSetAttributeValue(axUIElementPtr, new CFString("AXFrame").Handle, framePtr);
            Debug.WriteLine($"SetFrameAttibute done error {error}");

            //kAXErrorAttributeUnsupported = -25205

        }



        private IntPtr GetAttributePtr(IntPtr axUIElementPtr, string attributeName)
        {
            IntPtr attribPtr = IntPtr.Zero;
            AXUIElementCopyAttributeValue(axUIElementPtr, new CFString(attributeName).Handle, ref attribPtr);
            return attribPtr;
        }
    }
}
