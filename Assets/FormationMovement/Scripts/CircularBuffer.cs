using FormationMovement;
using UnityEngine;

/// <summary>
/// A fixed-size circular buffer for storing a sequence of elements in order. 
/// When full, it overwrites the oldest entries as new elements are added.
/// </summary>
public class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _index = 0;
    private int _count = 0;
    private const int MinEntriesToInterpolate = 2;

    /// <summary>
    /// Number of items currently stored in the buffer.
    /// </summary>
    public int Count => _count;
    
    /// <summary>
    /// Maximum number of items the buffer can store before overwriting old entries.
    /// </summary>
    public int Capacity => _buffer.Length;

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    public void Add(T item)
    {
        _buffer[_index] = item;
        _index = (_index + 1) % _buffer.Length;
        _count = Mathf.Min(_count + 1, _buffer.Length);
    }
    
    /// <summary>
    /// Retrieves the oldest item currently stored in the buffer.
    /// </summary>
    /// <returns>The oldest item in the buffer.</returns>
    public T GetOldest()
    {
        return _count < _buffer.Length ? _buffer[0] : _buffer[_index];
    }
    
    /// <summary>
    /// Attempts to interpolate a FormationLocation result using samples surrounding the given target time.
    /// Requires the buffer to store FormationLocation data.
    /// </summary>
    /// <param name="targetTime">The time at which to interpolate a position and rotation.</param>
    /// <param name="interpolated">
    /// Output parameter. If interpolation is successful, contains the interpolated FormationLocation.
    /// </param>
    /// <returns>True if interpolation was successful; false otherwise.</returns>
    public bool TryInterpolate(float targetTime, out FormationLocation interpolated)
    {
        interpolated = default;
        
        // at least two entries needed to interpolate
        if (_count < MinEntriesToInterpolate)
            return false;

        // iterate from newest to oldest to find bracket samples
        for (var i = 0; i < _count - 1; i++)
        {
            var idxNewer = (_index - 1 - i + Capacity) % Capacity;
            var idxOlder = (_index - 2 - i + Capacity) % Capacity;
            
            var newer = (FormationLocation)(object)_buffer[idxNewer];
            var older = (FormationLocation)(object)_buffer[idxOlder];

            if (older?.time <= targetTime && newer?.time >= targetTime)
            {
                var t = Mathf.InverseLerp(older.time, newer.time, targetTime);
                interpolated = new FormationLocation
                {
                    position = Vector3.Lerp(older.position, newer.position, t),
                    rotationY = Mathf.LerpAngle(older.rotationY, newer.rotationY, t),
                    time = targetTime
                };
                return true;
            }
        }

        return false;
    }
}