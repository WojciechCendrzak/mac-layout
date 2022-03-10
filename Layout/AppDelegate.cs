using System;
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

        [DllImport(ApplicationServices)]
        static extern IntPtr AXValueCreate(int valueType, ref bool boolValue);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXValueGetValue(IntPtr cfTypePtr, int valueType, ref bool boolValue);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXValueCreate(int valueType, ref CGRect cgRect);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXValueGetValue(IntPtr cfTypePtr, int valueType, ref CGRect cgRect);

        [DllImport(ApplicationServices)]
        static extern IntPtr AXUIElementSetAttributeValue(IntPtr element, IntPtr attribute, IntPtr value);
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

        public override void DidFinishLaunching(NSNotification notification)
        {
            if (AXAPIEnabled())
            {
                var app = GetWindowByName("iTerm");
                var pid = app.ValueForKey((NSString)"kCGWindowOwnerPID");
                var pidi = int.Parse(pid.ToString());
                GetUIElementsFromPID(pidi);
            }
            else
            {
                Console.WriteLine("API Disabled.");
            }
        }

        private void GetUIElementsFromPID(int pid)
        {
            var ax_app = AXUIElementCreateApplication(pid);
            GetUIElementChildren(ax_app);
        }

        private void GetUIElementChildren(IntPtr parentAXUIElementRef)
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
                    // Get every attributes
                    IntPtr namesPtr = IntPtr.Zero;
                    AXUIElementCopyAttributeNames(axUIElementPtr, ref namesPtr);
                    var attributes = NSArray.ArrayFromHandle<NSObject>(namesPtr);
                    foreach (var attrib in attributes)
                    {
                        Console.WriteLine(attrib.Description);
                        switch (attrib.Description)
                        {
                            case "AXTitle":
                            case "AXIdentifier":
                            case "AXHelp":
                            case "AXRole":
                            case "AXSubRole":
                            case "AXRoleDescription":
                            case "AXValue":
                                Console.WriteLine(attrib.Description + ":" + GetStringAttribute(axUIElementPtr, attrib.Description));
                                break;
                            case "AXEnabled":
                            case "AXFocused":
                                // I beg you please show me an example to get a boolean attribute.
                                break;
                            case "AXFrame":
                                Console.WriteLine(attrib.Description + ":" + GetFrameAttribute(axUIElementPtr));
                                break;
                        }
                    }

                    if (children.Length > 0)
                    {
                        GetUIElementChildren(axUIElementPtr);
                    }
                    CFRelease(axUIElementPtr);
                    i++;
                }
            }
        }

        private string GetStringAttribute(IntPtr axUIElementPtr, string attributeName)
        {
            return NSString.FromHandle(GetAttributePtr(axUIElementPtr, attributeName));
        }

        private CGRect GetFrameAttribute(IntPtr axUIElementPtr)
        {
            var attribPtr = GetAttributePtr(axUIElementPtr, "AXFrame");
            CGRect rect = new CGRect(0, 0, 0, 0);
            AXValueGetValue(attribPtr, kAXValueCGRectType, ref rect);
            AXValueCreate(kAXValueCGRectType, ref rect);
            return rect;
        }

        private IntPtr GetAttributePtr(IntPtr axUIElementPtr, string attributeName)
        {
            IntPtr attribPtr = IntPtr.Zero;
            AXUIElementCopyAttributeValue(axUIElementPtr, new CFString(attributeName).Handle, ref attribPtr);
            return attribPtr;
        }

    }
}
