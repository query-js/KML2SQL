namespace Kml2Sql.Mapping
{
    /// <summary>
    /// Type of Shape for MapFeatures.
    /// </summary>
    public enum ShapeType
    {
        Point,
        Polygon,
        LineString
    }

    /// <summary>
    /// Type of Polygon for MapFeatures
    /// </summary>
    public enum PolygonType
    {
        Geometry,
        Geography
    }
}