namespace Hack.io.J3D;

/// <summary>
/// Represents a J3D Keyframe
/// </summary>
public struct J3DKeyFrame(ushort time, float value, float ingoing = 0, float? outgoing = null) : IEquatable<J3DKeyFrame>
{
    /// <summary>
    /// The Time in the timeline that this keyframe is assigned to
    /// </summary>
    public ushort Time { get; set; } = time;
    /// <summary>
    /// The Value to set to
    /// </summary>
    public float Value { get; set; } = value;
    /// <summary>
    /// Tangents affect the interpolation between two consecutive keyframes
    /// </summary>
    public float IngoingTangent { get; set; } = ingoing;
    /// <summary>
    /// Tangents affect the interpolation between two consecutive keyframes
    /// </summary>
    public float OutgoingTangent { get; set; } = outgoing ?? ingoing;

    /// <summary>
    /// Converts the values based on a rotation multiplier
    /// </summary>
    /// <param name="RotationFraction">The byte in the file that determines the rotation fraction</param>
    /// <param name="Revert">Undo the conversion</param>
    public void ConvertRotation(byte RotationFraction, bool Revert = false)
    {
        float RotationMultiplier = (float)(Math.Pow(RotationFraction, 2) * (180.0 / 32768.0));
        Value = Revert ? Value / RotationMultiplier : Value * RotationMultiplier;
        IngoingTangent = Revert ? IngoingTangent / RotationMultiplier : IngoingTangent * RotationMultiplier;
        OutgoingTangent = Revert ? OutgoingTangent / RotationMultiplier : OutgoingTangent * RotationMultiplier;
    }


    /// <inheritdoc/>
    public readonly bool Equals(J3DKeyFrame other) =>
                Time == other.Time &&
                Value == other.Value &&
                IngoingTangent == other.IngoingTangent &&
                OutgoingTangent == other.OutgoingTangent;


    /// <inheritdoc/>
    public override readonly string ToString() => string.Format("Time: {0}, Value: {1}, Ingoing: {2}, Outgoing: {3}", Time, Value, IngoingTangent, OutgoingTangent);

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is J3DKeyFrame f && Equals(f);

    /// <inheritdoc/>
    public override readonly int GetHashCode() => HashCode.Combine(Time, Value, IngoingTangent, OutgoingTangent);


    public static bool operator ==(J3DKeyFrame left, J3DKeyFrame right) => left.Equals(right);
    public static bool operator !=(J3DKeyFrame left, J3DKeyFrame right) => !(left == right);
}
