FROM gcr.io/google-appengine/aspnetcore:3.1
COPY . /app
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080
ENTRYPOINT ["dotnet", "Alias.dll"]
