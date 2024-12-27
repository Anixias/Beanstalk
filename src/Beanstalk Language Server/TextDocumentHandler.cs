using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace BeanstalkLanguageServer;

internal sealed class TextDocumentHandler : TextDocumentSyncHandlerBase
{
	private readonly ILogger<TextDocumentHandler> logger;
	private readonly ILanguageServerConfiguration configuration;

	public static readonly TextDocumentSelector TextDocumentSelector =
		new(new TextDocumentFilter { Pattern = "**/*.bs" });

	public TextDocumentHandler(ILogger<TextDocumentHandler> logger, ILanguageServerConfiguration configuration)
	{
		this.logger = logger;
		this.configuration = configuration;
	}

	public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

	public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "beanstalk");

	public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
	{
		logger.LogInformation("textDocument/didOpen {TextDocumentUri}", request.TextDocument.Uri);
		return Unit.Task;
	}

	public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
	{
		logger.LogInformation("textDocument/didChange {TextDocumentUri}", request.TextDocument.Uri);
		return Unit.Task;
	}

	public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
	{
		logger.LogInformation("textDocument/didSave {TextDocumentUri}", request.TextDocument.Uri);
		return Unit.Task;
	}

	public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
	{
		logger.LogInformation("textDocument/didClose {TextDocumentUri}", request.TextDocument.Uri);
		return Unit.Task;
	}

	protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new TextDocumentSyncRegistrationOptions
		{
			DocumentSelector = TextDocumentSelector,
			Change = Change,
			Save = new SaveOptions { IncludeText = true }
		};
	}
}

internal class BeanstalkDocumentSymbolHandler// : IDocumentSymbolHandler
{
	/*public async Task<SymbolInformationOrDocumentSymbolContainer> Handle(DocumentSymbolParams request,
		CancellationToken cancellationToken)
	{
		// Todo: Get from editor source?
		var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(request)!, cancellationToken)
			.ConfigureAwait(false);
		
		
	}*/

	public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability,
		ClientCapabilities clientCapabilities) => new() { DocumentSelector = TextDocumentHandler.TextDocumentSelector };
}