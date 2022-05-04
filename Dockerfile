#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Leader Election In A Distributed System Using ZooKeeper/Leader Election In A Distributed System Using ZooKeeper.csproj"
RUN dotnet build "Leader Election In A Distributed System Using ZooKeeper/Leader Election In A Distributed System Using ZooKeeper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Leader Election In A Distributed System Using ZooKeeper/Leader Election In A Distributed System Using ZooKeeper.csproj" -c Release -o /app/publish

FROM base AS final
RUN apt update
RUN apt install curl grep -y
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Leader Election In A Distributed System Using ZooKeeper.dll"]

# Build using 
# docker build -t minhaz1217/ds-leader-election .

# Run using
# docker run --name leader-election -p 15001:80 -p 15002:443 -it --rm minhaz1217/ds-leader-election

# Push using
# docker push