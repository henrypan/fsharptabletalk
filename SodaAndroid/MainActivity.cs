using Android.App;
using Android.Widget;
using Android.OS;
using Microsoft.FSharp.Core;
using static SodaServiceDefinition;
using System;
using System.Reactive;
using Android.Support.V4.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;

namespace SodaAndroid
{
  [Activity (Label = "SodaAndroid", MainLauncher = true)]
  public class MainActivity : FragmentActivity
  {
    protected override void OnCreate (Bundle savedInstanceState)
    {
      base.OnCreate (savedInstanceState);

      SetContentView (Resource.Layout.permit_activity);

      // Start fetching from the server when a new activity starts
      SodaRequestParameters r = new SodaRequestParameters (SodaServiceDefinition.baseUrl, 0, 10, "latitude is not null and longitude is not null", "application_date desc");

      var actor = SodaServiceDefinition.actor;

      actor.Post (SodaCommand.NewRequest (r));
    }
  }
}
