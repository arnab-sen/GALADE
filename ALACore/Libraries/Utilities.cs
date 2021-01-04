using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using ProgrammingParadigms;

namespace Libraries
{
    public static class Utilities
    {
        /// <summary>
        /// <para>An extension method for objects, particularly anonymous objects, to get a named property.</para>
        /// <para>T : The type of the property to return.</para>
        /// <para>obj : The object containing the desired property.</para>
        /// <para>propertyName : the variable name of the property.</para>
        /// <para>Returns : A property cast as type T.</para>
        /// </summary>
        public static T GetProperty<T>(this object obj, string propertyName)
        {
            return (T)obj.GetType().GetProperty(propertyName)?.GetValue(obj);
        }

        /// <summary>
        /// <para>An extension method for objects, particularly anonymous objects, to set a named property.</para>
        /// <para>T : The type of the property to set.</para>
        /// <para>obj : The object containing the desired property.</para>
        /// <para>propertyName : the variable name of the property.</para>
        /// </summary>
        public static void SetProperty<T>(this object obj, string propertyName, T propertyValue)
        {
            obj.GetType().GetProperty(propertyName)?.SetValue(obj, propertyValue);
        }

        /// <summary>
        /// <para>A helper method to convert an object to any type T. The initial purpose of this was to enable casting from an anonymous type into an object while keeping the accessibility of its properties.</para>
        /// <para>T : The type to cast to.</para>
        /// <para>typeHolder : Any instance can be passed into this parameter and its type T will be extracted.</para>
        /// <para>obj : The object to cast.</para>
        /// <para>Returns : obj cast as type T.</para>
        /// </summary>
        public static T Cast<T>(this object obj, T typeHolder)
        {
            return (T)obj;
        }

        /// <summary>
        /// Connects a port of an instance to a virtual port in this class. Note: This only works for non-list output ports (e.g. this doesn't work with fanoutLists).
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="instancePortName"></param>
        /// <param name="virtualPort"></param>
        public static void ConnectToVirtualPort(object instance, string instancePortName, object virtualPort)
        {
            instance
                .GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(f => f.Name == instancePortName)
                ?.SetValue(instance, virtualPort);

            // var type = instance.GetType();
            // var field = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            //     .FirstOrDefault(f => f.Name == instancePortName);
            // var value = field?.GetValue(instance);
        }

        public static void RemoveUIElementFromParent(UIElement element)
        {
            try
            {
                (VisualTreeHelper.GetParent(element) as Panel).Children.Remove(element);
            }
            catch (Exception e)
            {

            }
        }

        public static void Log(this IEnumerable enumerable)
        {
            foreach (var element in enumerable)
            {
                Logging.Log(element.ToString());
            }
        }

        public static void RenderInNewWindow(UIElement render, int x = 0, int y = 0, int windowWidth = 720, int windowHeight = 480)
        {
            // Opens a window with a canvas containing the desired render at the desired position
            var window = new Window()
            {
                Width = windowWidth,
                Height = windowHeight,
                Topmost = true
            };

            var canvas = new Canvas() { Width = 500, Height = 500, Background = Brushes.White };
            window.Content = canvas;

            canvas.Children.Add(render);

            Canvas.SetLeft(render, x);
            Canvas.SetTop(render, y);

            window.Show();
        }

        public static Point RelativePositionToParent(UIElement child, UIElement parent)
        {
            return child.TranslatePoint(new Point(0, 0), parent);
        }

        public static void DebugTextWindow(string content)
        {
            var message = new Label() { Content = content };

            RenderInNewWindow(message);
        }

        public static string GetUniqueId()
        {
            var id = Guid.NewGuid().ToString();
            return Regex.Replace(id, @"[^\w\d]", ""); // Ensures that the id can be used in a variable name
        }

        /// <summary>
        /// Returns the working directory of the current application.
        /// </summary>
        /// <returns></returns>
        public static string GetApplicationDirectory()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            // var path = Directory.GetCurrentDirectory();

            return path;
        }

        public static string GetCurrentTime(bool includeDate = true)
        {
            var timeStr = $"{DateTime.Now:h:mm:ss tt}";
            if (includeDate) timeStr += $" {DateTime.Now:d}";
            return timeStr;
        }

        /// <summary>
        /// Reads and returns the contents of a file. If that file doesn't exist, a notFoundMessage is returned instead.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="notFoundMessage"></param>
        /// <returns></returns>
        public static string ReadFileSafely(string path, string notFoundMessage = "")
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(path);
                if (Directory.Exists(directoryPath))
                {
                    if (File.Exists(path))
                    {
                        var fileContents = File.ReadAllText(path);
                        return fileContents;
                    }
                }

                return notFoundMessage;
            }
            catch (Exception e)
            {
                return notFoundMessage;
            }
        }

        public static Brush BrushFromHex(string hex)
        {
            var hexString = hex;

            if (!hex.StartsWith("#")) hexString = "#" + hexString;

            var converter = new BrushConverter();
            var brush = converter.ConvertFromString(hexString) as SolidColorBrush;

            return brush;
        }

        /// <summary>
        /// Modifies all keys in a dictionary using a given function. If a condition predicate is supplied, then only keys satisfying that condition will be edited.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary">The source dictionary to edit.</param>
        /// <param name="keyTransform">The function to transform every key.</param>
        /// <param name="condition">The condition predicate to select keys with. If null, then all keys will be selected.</param>
        public static void EditKeys<TKey, TValue>(Dictionary<TKey, TValue> dictionary, Func<TKey, TKey> keyTransform, Func<TKey, bool> condition = null)
        {
            var keys = condition != null ? dictionary.Keys.Where(condition).ToList() : dictionary.Keys.ToList();

            var keyMap = new Dictionary<TKey, TKey>();
            foreach (var key in keys)
            {
                keyMap[key] = keyTransform(key);
            }

            var temp = new Dictionary<TKey, TValue>();
            foreach (var key in keys)
            {
                temp[key] = dictionary[key];
            }

            foreach (var key in keys)
            {
                dictionary.Remove(key);
            }

            foreach (var kvp in keyMap)
            {
                dictionary[keyMap[kvp.Key]] = temp[kvp.Key];
            }
        }
    }
}
