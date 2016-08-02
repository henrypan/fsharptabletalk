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
  public class OnMapReadyCallbackObservable : Java.Lang.Object, IOnMapReadyCallback
  {
    // The observable emits one GoogleMap object and then calls OnCompleted().
    public static IObservable<GoogleMap> Create (SupportMapFragment mapFragment, Context context)
    {
      return
        Observable
          .Create<GoogleMap> (arg => {
            mapFragment.GetMapAsync (new OnMapReadyCallbackObservable (arg));
            return Disposable.Empty;
          });
    }

    private readonly IObserver<GoogleMap> _mapObserver;

    // These should only be created by the #Create method
    private OnMapReadyCallbackObservable (IObserver<GoogleMap> obs)
    {
      _mapObserver = obs;
    }

    public void OnMapReady (GoogleMap googleMap)
    {
      _mapObserver.OnNext (googleMap);
      _mapObserver.OnCompleted ();
    }
  }
}

