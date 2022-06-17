using System.Text.Json.Serialization;

var app = WebApplication.Create(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

app.MapGet("/", () => "Let the battle begin!");
app.MapPost("/", (ArenaUpdate model) =>
{
    PlayerState self = GetSelf(model);

    // Priority 0, lots of threats
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

    // Priority 1, not lot of threats, have player in attack range
    List<PlayerState> playersInAttackRange = GetAttackRangePlayers(self, model);
    if (playersInAttackRange.Count > 0)
    {
        return "T";
    }

    // Priority 2, not lot of threats, no player in attack range
    var bestPursueDirection = GetBestPursueDirection(self, model);
    if (!String.IsNullOrEmpty(bestPursueDirection))
    {
        return bestPursueDirection;
    }

    // Last Priority, random
    return "F";
    //return new string[] { "F", "L", "R" }[Random.Shared.Next(0, 2)];
});

app.Run($"http://0.0.0.0:{port}");

string GetBestPursueDirection(PlayerState self, ArenaUpdate model)
{
    var dirL = GetDirectionIfTurned(self, "L");
    var dirR = GetDirectionIfTurned(self, "R");

    List<PlayerState> playersInAttackRightToTheLeft = GetPlayersInDirection(self, model, dirL);
    List<PlayerState> playersInAttackRightToTheRight = GetPlayersInDirection(self, model, dirR);
    if (playersInAttackRightToTheLeft.Count > 0 && playersInAttackRightToTheRight.Count > 0)
    {
        var turn = "L";
        var turnedDirection = GetDirectionIfTurned(self, turn);
        PlayerState closetPlayer = GetClosetPlayer(self, GetPlayersInDirection(self, model, turnedDirection));

        switch (turnedDirection)
        {
            case "N":
                if (closetPlayer.Direction != "S")
                {
                    return turn;
                }
                break;
            case "S":
                if (closetPlayer.Direction != "N")
                {
                    return turn;
                }
                break;
            case "E":
                if (closetPlayer.Direction != "W")
                {
                    return turn;
                }
                break;
            case "W":
                if (closetPlayer.Direction != "E")
                {
                    return turn;
                }
                break;
        }

        // If still not turned
        turn = "R";
        turnedDirection = GetDirectionIfTurned(self, turn);
        closetPlayer = GetClosetPlayer(self, GetPlayersInDirection(self, model, turnedDirection));

        switch (turnedDirection)
        {
            case "N":
                if (closetPlayer.Direction != "S")
                {
                    return turn;
                }
                break;
            case "S":
                if (closetPlayer.Direction != "N")
                {
                    return turn;
                }
                break;
            case "E":
                if (closetPlayer.Direction != "W")
                {
                    return turn;
                }
                break;
            case "W":
                if (closetPlayer.Direction != "E")
                {
                    return turn;
                }
                break;
        }
    }
    else if (playersInAttackRightToTheLeft.Count > 0)
    {
        return "L";
    }
    else if (playersInAttackRightToTheRight.Count > 0)
    {
        return "R";
    }

    return null;
}

List<PlayerState> GetPlayersInDirection(PlayerState self, ArenaUpdate model, string direction)
{
    switch (direction)
    {
        case "N":
            return model.Arena.State.Where(p => p.Value.X == self.X && p.Value.Y < self.Y).Select(p => p.Value).ToList();
        case "S":
            return model.Arena.State.Where(p => p.Value.X == self.X && p.Value.Y > self.Y).Select(p => p.Value).ToList();
        case "E":
            return model.Arena.State.Where(p => p.Value.Y == self.Y && p.Value.X > self.X).Select(p => p.Value).ToList();
        case "W":
            return model.Arena.State.Where(p => p.Value.Y == self.Y && p.Value.X < self.X).Select(p => p.Value).ToList();
    }

    return null;
}

PlayerState GetClosetPlayer(PlayerState self, List<PlayerState> players)
{
    PlayerState closetPlayer = players[0];
    foreach (PlayerState player in players)
    {
        var distance = Math.Abs((player.X + player.Y) - (self.X + self.Y));
        var lastClosetPlayerDistance = Math.Abs((closetPlayer.X + closetPlayer.Y) - (self.X + self.Y));

        if (distance < lastClosetPlayerDistance)
        {
            closetPlayer = player;
        }
    }

    //switch (direction)
    //{
    //    case "N":
    //        foreach (var player in players)
    //        {
    //            if (self.Y - player.Y < closetPlayer.Y)
    //            {
    //                closetPlayer = player;
    //            }
    //        }
    //        break;

    //    case "S":
    //        foreach (var player in players)
    //        {
    //            if (player.Y - self.Y < closetPlayer.Y)
    //            {
    //                closetPlayer = player;
    //            }
    //        }
    //        break;

    //    case "E":
    //        foreach (var player in players)
    //        {
    //            if (player.X - self.X < closetPlayer.X)
    //            {
    //                closetPlayer = player;
    //            }
    //        }
    //        break;

    //    case "W":
    //        foreach (var player in players)
    //        {
    //            if (self.X - player.X < closetPlayer.X)
    //            {
    //                closetPlayer = player;
    //            }
    //        }
    //        break;
    //}

    return closetPlayer;
}

List<PlayerState> GetPlayersInAttackRangeInDirection(PlayerState self, ArenaUpdate model, string turnDirection)
{
    var dir = GetDirectionIfTurned(self, turnDirection);
    var players = GetPlayersInDirection(self, model, dir);

    switch (dir)
    {
        case "N":
            return players.Where(p => p.X == self.X && self.Y - p.Y <= 3).ToList();
        case "S":
            return players.Where(p => p.X == self.X && p.Y - self.Y <= 3).ToList();
        case "E":
            return players.Where(p => p.Y == self.Y && p.X - self.X <= 3).ToList();
        case "W":
            return players.Where(p => p.Y == self.Y && self.X - p.X <= 3).ToList();
    }

    return new List<PlayerState>();
}

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
