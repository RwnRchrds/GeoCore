# GeoCore

**GeoCore** is a clean, modern, developer-friendly .NET library for geospatial math — including distance, bearing, movement, and polygon calculations.

## ✨ Features

- 📍 `GeoPoint` structure for latitude/longitude
- 📏 Great-circle distance and bearing calculations
- 🔁 Move a point by bearing + distance
- 📦 Unit conversion (distance, angle, area)
- 🧭 Polygon containment and area
- ✅ Fully unit-tested (NUnit)

## 📦 Installation

Coming soon to NuGet!

## 🚀 Usage

```csharp
var london = new GeoPoint(51.5074, -0.1278);
var amsterdam = new GeoPoint(52.3676, 4.9041);

double distanceKm = london.DistanceTo(amsterdam);
double bearing = london.BearingTo(amsterdam);