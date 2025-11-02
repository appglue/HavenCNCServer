# Get Travel Limits API

## Endpoint

**GET** `/api/CNCConfiguration/GetTravelLimits`

## Description

Retrieves the travel limits (software motion boundaries) for all configured axes on the CNC machine. Travel limits define the maximum and minimum positions that each axis can move to, preventing the machine from moving beyond safe boundaries.

## Request

No parameters required.

## Response Structure

### TypeScript Interface
```typescript
interface TravelLimitsResponse {
  Axes: AxisTravelLimits[];
  Message: string;
}

interface AxisTravelLimits {
  AxisNumber: number;    // Axis number (1-8)
  AxisLabel: string;     // Axis label (X, Y, Z, A, B, C, U, V, W)
  PlusLimit: number;     // Maximum position (plus direction)
  MinusLimit: number;    // Minimum position (minus direction)
}
```

### JSON Schema
```json
{
  "type": "object",
  "properties": {
    "Axes": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "AxisNumber": { "type": "integer", "minimum": 1, "maximum": 8 },
          "AxisLabel": { "type": "string" },
          "PlusLimit": { "type": "number" },
          "MinusLimit": { "type": "number" }
        },
        "required": ["AxisNumber", "AxisLabel", "PlusLimit", "MinusLimit"]
      }
    },
    "Message": { "type": "string" }
  },
  "required": ["Axes", "Message"]
}
```

## Example Response

### 3-Axis Machine (X, Y, Z)
```json
{
  "Axes": [
    {
      "AxisNumber": 1,
      "AxisLabel": "X",
      "PlusLimit": 24.0000,
      "MinusLimit": -1.0000
    },
    {
      "AxisNumber": 2,
      "AxisLabel": "Y",
      "PlusLimit": 16.0000,
      "MinusLimit": -1.0000
    },
    {
      "AxisNumber": 3,
      "AxisLabel": "Z",
      "PlusLimit": 1.0000,
      "MinusLimit": -8.0000
    }
  ],
  "Message": "Retrieved travel limits for 3 configured axes"
}
```

### 4-Axis Machine (X, Y, Z, A)
```json
{
  "Axes": [
    {
      "AxisNumber": 1,
      "AxisLabel": "X",
      "PlusLimit": 30.0000,
      "MinusLimit": 0.0000
    },
    {
      "AxisNumber": 2,
      "AxisLabel": "Y",
      "PlusLimit": 20.0000,
      "MinusLimit": 0.0000
    },
    {
      "AxisNumber": 3,
      "AxisLabel": "Z",
      "PlusLimit": 0.0000,
      "MinusLimit": -10.0000
    },
    {
      "AxisNumber": 4,
      "AxisLabel": "A",
      "PlusLimit": 360.0000,
      "MinusLimit": -360.0000
    }
  ],
  "Message": "Retrieved travel limits for 4 configured axes"
}
```

## Field Descriptions

### TravelLimitsResponse

| Field | Type | Description |
|-------|------|-------------|
| `Axes` | array | Array of travel limits for each configured axis |
| `Message` | string | Descriptive message indicating how many axes were retrieved |

### AxisTravelLimits

| Field | Type | Description |
|-------|------|-------------|
| `AxisNumber` | integer | Axis number (1-8) as configured in the system |
| `AxisLabel` | string | Standard axis label (X, Y, Z, A, B, C, U, V, W) |
| `PlusLimit` | number | Maximum position in the positive direction (machine units) |
| `MinusLimit` | number | Minimum position in the negative direction (machine units) |

## Important Notes

### Units
- Linear axes (X, Y, Z, U, V, W): Units are in inches or millimeters depending on machine configuration
- Rotary axes (A, B, C): Units are in degrees

### Coordinate System
- Travel limits are in **machine coordinates**
- Plus limit is typically the maximum positive position
- Minus limit is typically the maximum negative position (often negative value)
- For proper configuration: `PlusLimit > MinusLimit`

### Axis Configuration
- Only **configured and enabled** axes are returned
- Axis numbers 1-8 correspond to the physical axes on the machine
- Not all machines use all 8 axes - response only includes configured axes

### Travel Range
The actual travel range for an axis is: `PlusLimit - MinusLimit`

Example:
- X-axis with PlusLimit: 24.0, MinusLimit: -1.0
- Travel range: 24.0 - (-1.0) = 25.0 units

## Usage Examples

### JavaScript/TypeScript
```typescript
async function getTravelLimits() {
  try {
    const response = await fetch('/api/CNCConfiguration/GetTravelLimits');
    const data: TravelLimitsResponse = await response.json();
    
    console.log(data.Message);
    
    data.Axes.forEach(axis => {
      const range = axis.PlusLimit - axis.MinusLimit;
      console.log(`${axis.AxisLabel}-axis (${axis.AxisNumber}): ` +
                  `Range = ${range.toFixed(4)} ` +
                  `[${axis.MinusLimit.toFixed(4)} to ${axis.PlusLimit.toFixed(4)}]`);
    });
    
    return data;
  } catch (error) {
    console.error('Failed to get travel limits:', error);
    throw error;
  }
}

// Display limits in UI
function displayTravelLimits(limits: TravelLimitsResponse) {
  limits.Axes.forEach(axis => {
    const element = document.getElementById(`axis-${axis.AxisLabel}-limits`);
    if (element) {
      element.textContent = 
        `${axis.MinusLimit.toFixed(3)} to ${axis.PlusLimit.toFixed(3)}`;
    }
  });
}
```

### React Component
```typescript
import { useEffect, useState } from 'react';

interface AxisTravelLimits {
  AxisNumber: number;
  AxisLabel: string;
  PlusLimit: number;
  MinusLimit: number;
}

interface TravelLimitsResponse {
  Axes: AxisTravelLimits[];
  Message: string;
}

function TravelLimitsDisplay() {
  const [limits, setLimits] = useState<TravelLimitsResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/api/CNCConfiguration/GetTravelLimits')
      .then(res => res.json())
      .then(data => {
        setLimits(data);
        setLoading(false);
      })
      .catch(err => {
        console.error('Failed to load travel limits:', err);
        setLoading(false);
      });
  }, []);

  if (loading) return <div>Loading travel limits...</div>;
  if (!limits) return <div>Failed to load travel limits</div>;

  return (
    <div className="travel-limits">
      <h3>Machine Travel Limits</h3>
      <table>
        <thead>
          <tr>
            <th>Axis</th>
            <th>Minus Limit</th>
            <th>Plus Limit</th>
            <th>Total Range</th>
          </tr>
        </thead>
        <tbody>
          {limits.Axes.map(axis => (
            <tr key={axis.AxisNumber}>
              <td>{axis.AxisLabel}</td>
              <td>{axis.MinusLimit.toFixed(4)}</td>
              <td>{axis.PlusLimit.toFixed(4)}</td>
              <td>{(axis.PlusLimit - axis.MinusLimit).toFixed(4)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p className="message">{limits.Message}</p>
    </div>
  );
}
```

### C# Example
```csharp
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class TravelLimitsService
{
    private readonly HttpClient _httpClient;

    public TravelLimitsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TravelLimitsResponse> GetTravelLimitsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<TravelLimitsResponse>(
            "/api/CNCConfiguration/GetTravelLimits");
        
        return response;
    }

    public async Task DisplayLimitsAsync()
    {
        var limits = await GetTravelLimitsAsync();
        
        Console.WriteLine(limits.Message);
        Console.WriteLine("\nAxis Travel Limits:");
        Console.WriteLine("==================");
        
        foreach (var axis in limits.Axes)
        {
            double range = axis.PlusLimit - axis.MinusLimit;
            Console.WriteLine($"{axis.AxisLabel} (Axis {axis.AxisNumber}): " +
                            $"{axis.MinusLimit:F4} to {axis.PlusLimit:F4} " +
                            $"(Range: {range:F4})");
        }
    }
}
```

## Error Handling

### Possible Errors

**Connection Error:**
```json
{
  "error": "Failed to get travel limits: CNC connection not available"
}
```
- **Cause**: CNC system is not connected
- **Solution**: Ensure CNC connection is established before calling this endpoint

**API Restriction:**
```json
{
  "error": "Failed to get travel limits: API is restricted"
}
```
- **Cause**: CNC is in a state that restricts API access
- **Solution**: Check CNC system state and ensure it's ready for API calls

## Related Endpoints

- **POST /api/CNCConfiguration/ConfigureAxis** - Configure axis parameters including travel limits
- **GET /api/CNCMovement/GetCurrentPosition** - Get current machine position
- **GET /api/CNCSystem/IsConnectedToCentroid** - Check CNC connection status

## Use Cases

1. **Safety Validation**: Verify that commanded positions are within travel limits before executing moves
2. **UI Constraints**: Set min/max values for position input fields based on actual machine limits
3. **Work Envelope Visualization**: Display the machine's working area in 3D or 2D visualizations
4. **Job Validation**: Check if a G-code program stays within machine travel limits before running
5. **Setup Verification**: Confirm that travel limits are properly configured during machine setup

## Best Practices

1. **Cache Results**: Travel limits rarely change during operation - cache the response to avoid repeated API calls
2. **Validate Positions**: Always check that target positions are within travel limits before commanding movements
3. **Display Units**: Show units (inches/mm/degrees) next to limit values for clarity
4. **Safety Margin**: Consider using a safety margin (e.g., 0.1 inches) inside the actual limits for user operations
5. **Update on Configuration**: Re-fetch limits after any axis configuration changes
