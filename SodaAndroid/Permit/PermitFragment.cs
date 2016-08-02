using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Microsoft.FSharp.Core;
using static SodaServiceDefinition;

namespace SodaAndroid
{
  // The Register directive lets you use this class in Android XML - see
  // permit_activity.xml
  [Register ("com.restphone.PermitFragment")]
  public class PermitFragment : Android.Support.V4.App.Fragment
  {
    List<IDisposable> subscriptions = new List<IDisposable> () { };

    public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
      var result = inflater.Inflate (Resource.Layout.permit_fragment, container, false);

      var mapFragment = SupportMapFragment.NewInstance (); // Standard Google map call, nothing specific to our app

      // We publish the OnMapReadyCallbackObservable so more than one subscriber can use it.  
      // Right now, there are two subscribers; one that puts markers on the map and another
      // one that does something when the user clicks on the map.
      var mapFetchedConnectable = OnMapReadyCallbackObservable.Create (mapFragment, Context).Publish ();

      subscriptions.Add (mapFetchedConnectable.Subscribe (ZoomToSeattle));

      // Before we do much of anything, we need both a GoogleMap and some building permits to display on
      // that map.
      var mapAndPermits =
        mapFetchedConnectable.CombineLatest (SodaServiceDefinition.buildingPermitsReplay, (a, b) => {
          return new { map = a, permits = b };
        });

      IObservable<BuildingPermit []> markerPoints =
        mapAndPermits
          .ObserveOn (System.Threading.SynchronizationContext.Current)
          .Select (x => {
            //Console.WriteLine ("fnx got permits: {0} {1}", x.permits, x.permits.Length);
            return MapPointObservable.Create (x.permits, x.map);
          })
          .Switch ();

      IObservable<LatLng> mapClickEvents =
        mapFetchedConnectable
          .SelectMany (map => {
            return Observable.FromEventPattern<GoogleMap.MapClickEventArgs> (h => map.MapClick += h, h => map.MapClick -= h);
          })
          .Select (c => { return c.EventArgs.Point; });

      // clicking on the map asks the service for the items closest to that click
      subscriptions.Add (mapClickEvents.Subscribe (click => {
        var c = SodaCommand.NewGetOrderedItems ((decimal)click.Latitude, (decimal)click.Longitude);
        SodaServiceDefinition.actor.Post (c);
      }));

      // Creating the observables doesn't actually do anything; think of them as lazy.
      // Actually creating the subscriptions is what 
      subscriptions.Add (markerPoints.Subscribe ());
      subscriptions.Add (mapFetchedConnectable.Connect ());

      var transaction = FragmentManager.BeginTransaction ();
      transaction.Add (Resource.Id.mapFragmentContainer, mapFragment);
      transaction.Commit ();

      return result;
    }

    public void ZoomToSeattle (GoogleMap googleMap)
    {
      var mMap = googleMap;

      float mx = mMap.MaxZoomLevel;
      float mn = mMap.MinZoomLevel;
      float zoom = mMap.MaxZoomLevel - (mMap.MaxZoomLevel - mMap.MinZoomLevel) / 2.0f;

      LatLng seattle = new LatLng (47.66330435, -122.3755007);
      var cam = CameraUpdateFactory.NewLatLngZoom (seattle, zoom);
      mMap.MoveCamera (cam);

      mMap.BuildingsEnabled = true;
    }
  }
}

