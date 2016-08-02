using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.FSharp.Core;
using static SodaServiceDefinition;

namespace SodaAndroid
{
  public class MapPointObservable
  {
    // Subscribing to this observable will put markers on the map
    // for each item in items.  Unsubscribing removes those markers.
    // Emit the items that get mapped so you can chain observables.
    public static IObservable<BuildingPermit []> Create (BuildingPermit [] items, GoogleMap map)
    {
      return
        Observable.Create<BuildingPermit []> (subscriber => {
          //Console.WriteLine ($"fnx create MapPointObservable {items} {map}");
          List<Marker> markers = new List<Marker> ();
          //Console.WriteLine ($"fnx MapPointObservable add {items.Length} map markers");
          foreach (BuildingPermit p in items) {
            var x = AddMapPointer (map, p);
            markers.Add (x);
          }
          subscriber.OnNext (items);
          return
            Disposable.Create (() => {
              //Console.WriteLine ($"fnx MapPointObservable remove {markers.Count} map markers");
              foreach (Marker m in markers) {
                m.Remove ();
              }
            });
        });
    }

    static Marker AddMapPointer (GoogleMap map, BuildingPermit permit)
    {
      var latitude = (double)permit.Latitude;
      var longitude = (double)permit.Longitude;
      var markerOptions =
        new MarkerOptions ()
          .SetPosition (new LatLng (latitude, longitude))
          .SetTitle (permit.PermitType);
      //Console.WriteLine ($"fnx MapPointObservable add marker at {latitude} {longitude}");
      return map.AddMarker (markerOptions);
    }
  }
}

