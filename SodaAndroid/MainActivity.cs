using Android.App;
using Android.Widget;
using Android.OS;
using Microsoft.FSharp.Core;
using static SodaServiceDefinition;
using System;
using System.Reactive;
namespace SodaAndroid
{
  [Activity (Label = "SodaAndroid", MainLauncher = true, Icon = "@mipmap/icon")]
  public class MainActivity : Activity
  {
    int count = 1;
    IDisposable items;

    protected override void OnCreate (Bundle savedInstanceState)
    {
      base.OnCreate (savedInstanceState);

      // Set our view from the "main" layout resource
      SetContentView (Resource.Layout.Main);

      // Get our button from the layout resource,
      // and attach an event to it
      Button button = FindViewById<Button> (Resource.Id.myButton);

      button.Click += delegate { button.Text = string.Format ("{0} clicks!", count++); };

      var q = FSharpOption<string>.Some ("foo");

      System.Console.WriteLine ("q is " + q);

      var actor = SodaServiceDefinition.actor;
      items = SodaServiceDefinition.publishedEvent.Subscribe (xx => System.Console.WriteLine ("gotx: " + xx));

      actor.Post (SodaCommand.NewRequest ("foo", "adsf"));
    }
  }
}


