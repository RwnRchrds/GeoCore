# GeoCore

**GeoCore** is a clean, modern, dependency-light .NET library for geospatial maths — distance,
bearing, movement, areas, routes, polygons and the formats you need to get data in and out.

[![CI](https://github.com/RwnRchrds/GeoCore/actions/workflows/ci.yml/badge.svg)](https://github.com/RwnRchrds/GeoCore/actions/workflows/ci.yml)

- Targets **.NET 8** and **.NET Standard 2.0** (so it also runs on .NET Framework 4.6.1+, Unity and Xamarin)
- No third-party dependencies
- Every public member is documented and unit-tested

## Features

| Area | What you get |
| --- | --- |
| **Points** | `GeoPoint` — a validated, immutable lat/lon value type with parsing and formatting |
| **Spherical geodesy** | Great-circle distance, initial and final bearing, midpoint, true interpolation, destination |
| **Ellipsoidal geodesy** | Vincenty's direct and inverse formulae on WGS-84, accurate to well under a millimetre |
| **Rhumb lines** | Constant-bearing distance, bearing, destination and midpoint |
| **Track maths** | Cross-track and along-track distance, closest point on a segment |
| **Shapes** | `GeoBoundingBox` (antimeridian-aware), `GeoPolygon` (with holes), `GeoCircle`, `GeoRoute` |
| **Units** | Distance, area and angle conversion using exact international definitions |
| **Interop** | GeoJSON, WKT, geohash, Google encoded polyline, DMS/DDM parsing and formatting |

## Installation

```bash
dotnet add package GeoCore
```

## Quick start

```csharp
using GeoCore.Core;
using GeoCore.Extensions;
using GeoCore.Units;

var london = new GeoPoint(51.5074, -0.1278);
var paris  = new GeoPoint(48.8566, 2.3522);

double km      = london.DistanceTo(paris);                        // 343.56
double miles   = london.DistanceTo(paris, DistanceUnit.Miles);    // 213.48
double bearing = london.BearingTo(paris);                         // 148.12°

GeoPoint halfway = london.MidpointTo(paris);
GeoPoint arrival = london.Move(100, bearing);                     // 100 km down that bearing
```

Coordinates are validated on construction, so a `GeoPoint` is always a real place:

```csharp
new GeoPoint(512, 999);                        // throws ArgumentOutOfRangeException
GeoPoint.TryCreate(512, 999, out var p);       // false
GeoPoint.Clamped(512, 999);                    // 90°N, 81°W  — clamp latitude, wrap longitude
GeoPoint.Normalized(100, 0);                   // 80°N, -180° — wrap over the pole
```

## Accuracy: spherical vs ellipsoidal

`DistanceTo` uses the haversine formula on a sphere. It is fast and within about 0.5% — fine for
"how far is the nearest shop". When you need survey-grade numbers, use the WGS-84 ellipsoid:

```csharp
london.DistanceTo(paris);                            // 343.557 km  (spherical)
london.GeodesicDistanceTo(paris);                    // 343.923 km  (Vincenty, WGS-84)
```

Vincenty's inverse formula does not converge for very nearly antipodal points. `GeodesicDistanceTo`
throws `GeodesyConvergenceException` there; use `TryGeodesicDistanceTo` if you would rather fall
back to the spherical answer.

## Great circles, rhumb lines and tracks

```csharp
// A great circle is the shortest path, but its bearing changes as you fly it.
london.BearingTo(paris);        // 148.12° on departure
london.FinalBearingTo(paris);   // 150.02° on arrival

// A rhumb line is longer but holds one bearing the whole way.
london.RhumbDistanceTo(paris);  // 343.572 km
london.RhumbBearingTo(paris);   // 149.08°

// How far off a planned track are we, and how far along it?
var plane = new GeoPoint(50.0, 1.0);
plane.CrossTrackDistanceTo(london, paris);   // signed: negative is right of the track
plane.AlongTrackDistanceTo(london, paris);   // negative if it lies behind the start
plane.DistanceToSegment(london, paris);      // to the bounded segment, not the infinite circle
```

`IntermediatePointTo` is true spherical interpolation, not a linear blend of latitude and
longitude, so the result really does lie on the shortest path:

```csharp
london.IntermediatePointTo(paris, 0.25);    // a quarter of the way along the arc
```

## Shapes

```csharp
// A bounding box that knows about the antimeridian.
var box = new GeoPoint(-18, 179.9).GetBoundingBox(50);
box.CrossesAntimeridian;                       // true
box.Contains(new GeoPoint(-18, -179.95));      // true — 50 km east, over the dateline

GeoBoundingBox.FromPoints(points);             // the *narrowest* enclosing box, wrapping if that helps
box.Union(other);  box.Intersects(other);  box.Expand(10);  box.Center;  box.Area();
```

```csharp
// A polygon, optionally with holes punched in it.
var park = new GeoPolygon(
    outerRing,
    holes: new[] { lakeRing });

park.Contains(point);           // ray casting; holes are excluded
park.Area(AreaUnit.Hectares);   // spherical excess, holes subtracted
park.Perimeter();
park.Centroid();
park.Simplify(tolerance: 50, DistanceUnit.Meters);
```

```csharp
var circle = new GeoCircle(london, 5, DistanceUnit.Miles);
circle.Contains(point);
circle.BoundingBox;           // a true bound, correct even around a pole
circle.ToPolygon(segments: 64);
```

## Routes

```csharp
var route = new GeoRoute(gpsTrack);

route.TotalDistance(DistanceUnit.Miles);
route.MoveAlongRouteByFraction(0.5);        // the halfway point
route.InterpolatePoints(100);               // 100 evenly spaced points
route.Simplify(10, DistanceUnit.Meters);    // thin a GPS track (Ramer-Douglas-Peucker)

var position = route.ClosestPointTo(somewhere);
position.SegmentIndex;        // which leg it falls on
position.DistanceAlongRoute;  // how far along
position.DistanceFromRoute;   // how far off
```

Segment lengths are measured once and cached, so repeatedly querying positions along a long track
does not re-measure it.

## Parsing and formatting

```csharp
GeoPoint.Parse("51.5074, -0.1278");
GeoPoint.Parse("51°30'26.64\"N, 0°07'40.08\"W");
GeoPoint.Parse("N51 30 26.64 W0 7 40.08");
GeoPoint.TryParse(userInput, out var point);

point.ToString();          // GeoPoint(Latitude: 51.507400, Longitude: -0.127800)
point.ToString("D");       // 51.507400, -0.127800
point.ToString("DMS");     // 51°30'26.64"N, 0°07'40.08"W
point.ToString("DM");      // 51°30.4440'N, 0°07.6680'W
```

All formatting uses the invariant culture, so output does not change with the machine's locale.

## Interop

```csharp
using GeoCore.Formats;

GeoJson.Write(polygon);                     // {"type":"Polygon","coordinates":[[...]]}
GeoJson.ReadPolygon(json);                  // also accepts a Feature wrapper

Wkt.Write(route);                           // LINESTRING (-0.1278 51.5074, ...)
Wkt.ReadPolygon("POLYGON ((0 0, 1 0, ...))");

Geohash.Encode(london, precision: 9);       // "gcpvj0duq"
Geohash.Neighbors(hash);                    // the surrounding 3x3 block, for proximity search

EncodedPolyline.Encode(route.Points);       // Google's compact polyline format
EncodedPolyline.Decode(encoded, precision: 6);
```

> **Note:** GeoJSON and WKT both write **longitude before latitude**. GeoCore's readers check the
> ranges and say so explicitly if the values look transposed.

## Units

Conversions route through metres (or square metres) using exact international definitions — 1 mile
is 1609.344 m, 1 acre is 4046.8564224 m² — so round-tripping a value through any pair of units is
accurate to floating-point rounding.

```csharp
DistanceConverter.Convert(1, DistanceUnit.Miles, DistanceUnit.Kilometers);  // exactly 1.609344
AreaConverter.Convert(640, AreaUnit.Acres, AreaUnit.SquareMiles);           // exactly 1
```

## A note on edge cases

Geospatial code fails in predictable places, so these are handled explicitly and covered by tests:

- **The antimeridian.** Bounding boxes may wrap; polygon rings are unwrapped into a continuous run
  before any test, so a shape straddling the dateline measures correctly.
- **The poles.** A bounding box around a pole spans every longitude rather than dividing by a
  cosine that has gone to zero.
- **Boundaries.** `GeoPolygon.Contains` uses a half-open edge rule, so the answer never depends on
  which vertex the ring happens to start at. For points sitting exactly on an edge, use
  `IsOnBoundary`, since ray casting cannot give a meaningful answer there.
- **Rounding.** DMS values are rounded before being split into components, so you never see
  `0°59'60.00"`.

## Building

```bash
dotnet build
dotnet test
dotnet pack GeoCore/GeoCore.csproj -c Release
```

## Licence

MIT — see [LICENSE.txt](LICENSE.txt).
