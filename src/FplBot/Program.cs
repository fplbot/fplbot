using FplBot.Hosting;

var services = args.ParseServices();
await FplBotApplication.RunAsync(args, services);
