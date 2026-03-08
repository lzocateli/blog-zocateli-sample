// ==========================================================================
// Artigo: Arquitetura de Software: GoF, Padrões e Microserviços
// URL: /posts/2025/arquitetura-software-gof-padroes-cloud-microservicos/
// Padrões Criacionais: Factory Method, Abstract Factory, Builder,
//                      Prototype, Singleton
// ==========================================================================

namespace BlogSamples.DesignPatterns.Creational;

// -----------------------------------------------------------------------
// Factory Method
// "Quero criar algo, mas não quero saber COMO é criado"
// -----------------------------------------------------------------------

public interface ICanal
{
    void Enviar(string mensagem);
}

public class CanalEmail : ICanal
{
    public void Enviar(string mensagem) { /* envio por email */ }
}

public class CanalSms : ICanal
{
    public void Enviar(string mensagem) { /* envio por SMS */ }
}

public abstract class NotificadorBase
{
    // Factory Method: subclasses definem qual canal usar
    protected abstract ICanal CriarCanal();

    public void Notificar(string mensagem)
    {
        var canal = CriarCanal();  // criação delegada à subclass
        canal.Enviar(mensagem);
    }
}

public class NotificadorEmail : NotificadorBase
{
    protected override ICanal CriarCanal() => new CanalEmail();
}

public class NotificadorSms : NotificadorBase
{
    protected override ICanal CriarCanal() => new CanalSms();
}

// Uso:
// NotificadorBase notificador = ambiente == "prod"
//     ? new NotificadorEmail()
//     : new NotificadorSms();
// notificador.Notificar("Pedido confirmado");

// -----------------------------------------------------------------------
// Abstract Factory
// Componentes que devem ser consistentes entre si (mesma "família")
// -----------------------------------------------------------------------

public interface IBotao { }
public interface IInput { }
public interface ICard { }

public class BotaoLight : IBotao { }
public class InputLight : IInput { }
public class CardLight : ICard { }

public class BotaoDark : IBotao { }
public class InputDark : IInput { }
public class CardDark : ICard { }

public interface IUiFactory
{
    IBotao CriarBotao();
    IInput CriarInput();
    ICard CriarCard();
}

public class LightThemeFactory : IUiFactory
{
    public IBotao CriarBotao() => new BotaoLight();
    public IInput CriarInput() => new InputLight();
    public ICard CriarCard() => new CardLight();
}

public class DarkThemeFactory : IUiFactory
{
    public IBotao CriarBotao() => new BotaoDark();
    public IInput CriarInput() => new InputDark();
    public ICard CriarCard() => new CardDark();
}

// A tela usa a factory — nunca instancia diretamente:
public class TelaCheckout(IUiFactory factory)
{
    private readonly IBotao _btnConfirmar = factory.CriarBotao();
    private readonly IInput _inputCartao  = factory.CriarInput();
}

// -----------------------------------------------------------------------
// Builder
// Resolve o "telescoping constructor" problem
// -----------------------------------------------------------------------

public enum PrioridadePedido { Normal, Expressa }

public class ItemPedido(int produtoId, int quantidade)
{
    public int ProdutoId { get; } = produtoId;
    public int Quantidade { get; } = quantidade;
}

public class PedidoCriacional
{
    public int ClienteId { get; set; }
    public List<ItemPedido> Itens { get; set; } = [];
    public decimal Desconto { get; set; }
    public PrioridadePedido Prioridade { get; set; } = PrioridadePedido.Normal;
    public string? Rua { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }

    public void Validar()
    {
        if (ClienteId <= 0) throw new InvalidOperationException("ClienteId obrigatório");
        if (Itens.Count == 0) throw new InvalidOperationException("Pelo menos um item obrigatório");
    }
}

public class PedidoBuilder
{
    private readonly PedidoCriacional _pedido = new();

    public PedidoBuilder ParaCliente(int clienteId)
        { _pedido.ClienteId = clienteId; return this; }

    public PedidoBuilder ComItem(int produtoId, int quantidade)
        { _pedido.Itens.Add(new ItemPedido(produtoId, quantidade)); return this; }

    public PedidoBuilder ComDesconto(decimal percentual)
        { _pedido.Desconto = percentual; return this; }

    public PedidoBuilder ComEnderecoEntrega(string rua, string cidade, string estado)
        { _pedido.Rua = rua; _pedido.Cidade = cidade; _pedido.Estado = estado; return this; }

    public PedidoBuilder ComPrioridadeExpressa()
        { _pedido.Prioridade = PrioridadePedido.Expressa; return this; }

    public PedidoCriacional Build()
    {
        _pedido.Validar();  // validação centralizada no Build
        return _pedido;
    }
}

// Uso:
// var pedido = new PedidoBuilder()
//     .ParaCliente(clienteId: 1)
//     .ComItem(produtoId: 10, quantidade: 2)
//     .ComItem(produtoId: 15, quantidade: 1)
//     .ComDesconto(10)
//     .ComEnderecoEntrega("Rua A", "São Paulo", "SP")
//     .ComPrioridadeExpressa()
//     .Build();

// -----------------------------------------------------------------------
// Prototype
// Útil quando criar do zero é caro (consulta ao banco, inicialização pesada)
// -----------------------------------------------------------------------

public abstract class Componente
{
    public abstract Componente Clone();
    public string Nome { get; set; } = "";
    public Dictionary<string, string> Configuracoes { get; set; } = [];
}

public class ComponenteNginx : Componente
{
    public override Componente Clone()
    {
        var clone = (ComponenteNginx)MemberwiseClone();
        // Deep copy das configurações mutáveis
        clone.Configuracoes = new Dictionary<string, string>(Configuracoes);
        return clone;
    }
}

// Uso:
// var templateNginx = new ComponenteNginx { Nome = "nginx-template" };
// templateNginx.Configuracoes["porta"] = "80";
// var nginx1 = (ComponenteNginx)templateNginx.Clone();
// nginx1.Nome = "nginx-producao";

// -----------------------------------------------------------------------
// Singleton
// Em .NET moderno, prefira DI com Lifetime.Singleton
// -----------------------------------------------------------------------

public sealed class GerenciadorCache
{
    private static readonly Lazy<GerenciadorCache> _instancia =
        new(() => new GerenciadorCache());

    private GerenciadorCache() { }

    public static GerenciadorCache Instancia => _instancia.Value;

    public object? Obter(string chave) { return null; }
    public void Definir(string chave, object valor) { /* ... */ }
}

// ✅ Em ASP.NET Core: use services.AddSingleton<IGerenciadorCache, GerenciadorCache>()
