using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Domain
{
    public class GameEntity
    {
        [BsonElement]
        private readonly List<Player> players;

        public GameEntity(int turnsCount)
            : this(Guid.Empty, GameStatus.WaitingToStart, turnsCount, 0, new List<Player>())
        {
        }
        
        [BsonConstructor]
        public GameEntity(Guid id, GameStatus status, int turnsCount, int currentTurnNumber, List<Player> players)
        {
            Id = id;
            Status = status;
            TurnsCount = turnsCount;
            CurrentTurnNumber = currentTurnNumber;
            this.players = players;
        }

        public Guid Id
        {
            get;
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local For MongoDB
            private set;
        }
        
        [BsonElement]
        public IReadOnlyList<Player> Players => players.AsReadOnly();
        
        [BsonElement]
        public int TurnsCount { get; }

        public int CurrentTurnNumber { get; private set; }

        public GameStatus Status { get; private set; }

        public void AddPlayer(UserEntity user)
        {
            if (Status != GameStatus.WaitingToStart)
                throw new ArgumentException(Status.ToString());
            players.Add(new Player(user.Id, user.Login));
            if (Players.Count == 2)
                Status = GameStatus.Playing;
        }

        public bool IsFinished()
        {
            return CurrentTurnNumber >= TurnsCount
                   || Status == GameStatus.Finished
                   || Status == GameStatus.Canceled;
        }

        public void Cancel()
        {
            if (!IsFinished())
                Status = GameStatus.Canceled;
        }
        
        public bool HaveDecisionOfEveryPlayer => Players.All(p => p.Decision.HasValue);

        public void SetPlayerDecision(Guid userId, PlayerDecision decision)
        {
            if (Status != GameStatus.Playing)
                throw new InvalidOperationException(Status.ToString());
            foreach (var player in Players.Where(p => p.UserId == userId))
            {
                if (player.Decision.HasValue)
                    throw new InvalidOperationException(player.Decision.ToString());
                player.Decision = decision;
            }
        }

        public GameTurnEntity FinishTurn()
        {
            if (Players.Count < 2 || !Players[0].Decision.HasValue || !Players[1].Decision.HasValue)
                throw new InvalidOperationException("Both players must have a decision to finish the turn.");

            Guid? turnWinnerId = null;
            
            var player1 = Players[0];
            var player2 = Players[1];

            var player1Decision = player1.Decision.Value;
            var player2Decision = player2.Decision.Value;

            if (player1Decision.Beats(player2Decision))
            {
                player1.Score++;
                turnWinnerId = player1.UserId;
            }
            else if (player2Decision.Beats(player1Decision))
            {
                player2.Score++;
                turnWinnerId = player2.UserId;
            }

            var playerTurnResults = new List<PlayerTurnResult>();

            var player1Result = new PlayerTurnResult
            {
                UserId = player1.UserId,
                Name = player1.Name,
                Decision = player1Decision,
                Result = turnWinnerId == null ? TurnResult.Draw : turnWinnerId == player1.UserId ? TurnResult.Won : TurnResult.Lost
            };
            playerTurnResults.Add(player1Result);

            var player2Result = new PlayerTurnResult
            {
                UserId = player2.UserId,
                Name = player2.Name,
                Decision = player2Decision,
                Result = turnWinnerId == null ? TurnResult.Draw : turnWinnerId == player2.UserId ? TurnResult.Won : TurnResult.Lost
            };
            playerTurnResults.Add(player2Result);


            var gameTurnEntity = new GameTurnEntity(Id, CurrentTurnNumber + 1);
            gameTurnEntity.Players.AddRange(playerTurnResults);
            gameTurnEntity.WinnerId = turnWinnerId;
            
            foreach (var player in Players)
                player.Decision = null;
            
            CurrentTurnNumber++;
            
            if (CurrentTurnNumber >= TurnsCount)
                Status = GameStatus.Finished;
            
            return gameTurnEntity;
        }
    }
}