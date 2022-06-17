using System.Text.Json.Serialization;

var app = WebApplication.Create(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

app.MapGet("/", () => "Let the battle begin!");
app.MapPost("/", (ArenaUpdate model) =>
{
    PlayerState self = GetSelf(model);

    List<PlayerState> playersInAttackRange = GetAttackRangePlayers(self, model);
    if (playersInAttackRange.Count > 0)
    {
        return "T";
    }

    List<PlayerState> threats = GetImmediateThreats(self, model);
    if (threats.Count() >= 2)
    {
        List<int> nextPosFront = GetNextPos(self, model, self.Direction, out bool isWallFront, out bool isOccupiedFront);

        if (!isWallFront && !isOccupiedFront)
        {
            // Move forward
            return "F";
        }
        else
        {
            List<int> nextPosLeft = GetNextPos(self, model, GetDirectionIfTurned(self, "L"), out bool isWallLeft, out bool isOccupiedLeft);

            if (!isWallLeft && !isOccupiedLeft)
            {
                return "L";
            }
            else
            {
                return "R";
            }
        }
    }


    // RANDOM?
    return "F";
});

app.Run($"http://0.0.0.0:{port}");

string GetDirectionIfTurned(PlayerState self, string turnDirection)
{
    if (turnDirection == "L")
    {
        switch (self.Direction)
        {
            case "N":
                return "W";
            case "S":
                return "E";
            case "E":
                return "N";
            case "W":
                return "S";
        }
    }
    else if (turnDirection == "R")
    {
        switch (self.Direction)
        {
            case "N":
                return "E";
            case "S":
                return "W";
            case "E":
                return "S";
            case "W":
                return "N";
        }
    }

    return null;
}

List<PlayerState> GetImmediateThreats(PlayerState self, ArenaUpdate model)
{
    var threats = new List<PlayerState>();
    foreach (PlayerState player in model.Arena.State.Values)
    {
        // At this player's perspective, check if he can attack us
        if (IsInAttackRange(player, model, self))
        {
            threats.Add(player);
        }
    }

    return threats;
}

List<PlayerState> GetThreatAtPos(List<int> pos, ArenaUpdate model)
{
    PlayerState futureSelf = new PlayerState(pos[0], pos[1], "N", false, 0);

    var threats = new List<PlayerState>();
    foreach (PlayerState player in model.Arena.State.Values)
    {
        // At this player's perspective, check if he can attack us
        if (IsInAttackRange(player, model, futureSelf))
        {
            threats.Add(player);
        }
    }

    return threats;
}

List<PlayerState> GetAttackRangePlayers(PlayerState self, ArenaUpdate model)
{
    var inRangePlayers = model.Arena.State.Where(p => IsInAttackRange(self, model, p.Value)).Select(p => p.Value).ToList();

    return inRangePlayers;
}

List<int> GetNextPos(PlayerState self, ArenaUpdate model, string directionToSelf, out bool isWall, out bool isOccupied)
{
    isWall = false;
    isOccupied = false;

    var x = self.X;
    var y = self.Y;

    switch (directionToSelf)
    {
        case "N":
            y -= 1;
            break;
        case "S":
            y += 1;
            break;
        case "E":
            x += 1;
            break;
        case "W":
            x -= 1;
            break;
    }

    if (x < 0)
    {
        x = 0;
        isWall = true;
    }
    else if (x > model.Arena.Dims[0])
    {
        x = model.Arena.Dims[0];
        isWall = true;
    }

    if (y < 0)
    {
        y = 0;
        isWall = true;
    }
    else if (y > model.Arena.Dims[1])
    {
        y = model.Arena.Dims[1];
        isWall = true;
    }

    if (model.Arena.State.Any(p => p.Value.X == x && p.Value.Y == y))
    {
        isOccupied = true;
    }

    return new List<int>() { x, y };
}

bool IsInAttackRange(PlayerState self, ArenaUpdate model, PlayerState player)
{
    switch (self.Direction)
    {
        case "N":
            if (player.X == self.X)
            {
                if (player.Y - self.Y <= -3)
                {
                    return true;
                }
            }
            break;

        case "S":
            if (player.X == self.X)
            {
                if (player.Y - self.Y <= 3)
                {
                    return true;
                }
            }
            break;

        case "E":
            if (player.Y == self.Y)
            {
                if (player.X - self.X <= 3)
                {
                    return true;
                }
            }
            break;

        case "W":
            if (player.Y == self.Y)
            {
                if (player.X - self.X <= -3)
                {
                    return true;
                }
            }
            break;
    }

    return false;
}

PlayerState GetSelf(ArenaUpdate model)
{
    var name = model.Links.Self.Href;
    return model.Arena.State.First(rec => rec.Key == name).Value;
}

bool WithinBounds(List<int> dims, int x, int y)
{
    var width = dims[0];
    var height = dims[1];

    if (x > width || x < 0)
    {
        return false;
    }
    else if (y > height || y < 0)
    {
        return false;
    }

    return true;
}

internal record ArenaUpdate([property: JsonPropertyName("_links")] Links Links, Arena Arena);

internal record Links(Self Self);

internal record Self(string Href);

internal record Arena(List<int> Dims, Dictionary<string, PlayerState> State);

internal record PlayerState(int X, int Y, string Direction, bool WasHit, int Score);



//List<int> GetEscapeRoute(PlayerState self, List<PlayerState> threats, ArenaUpdate model)
//{
//    bool threatSouth = false;
//    bool threatNorth = false;
//    bool threatEast = false;
//    bool threatWest = false;

//    var threatDirections = new List<string>();
//    foreach (PlayerState threat in threats)
//    {
//        switch (threat.Direction)
//        {
//            // Below
//            case "N":
//                threatSouth = true;
//                break;
//            case "S":
//                threatNorth = true;
//                break;
//            case "E":
//                threatWest = true;
//                break;
//            case "W":
//                threatEast = true;
//                break;
//        }
//    }


//}
