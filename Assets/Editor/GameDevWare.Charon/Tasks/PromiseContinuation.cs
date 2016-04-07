
namespace Assets.Editor.GameDevWare.Charon.Tasks
{
	public delegate void ActionContinuation(Promise promise);
	public delegate void ActionContinuation<PromiseT>(Promise<PromiseT> promise);

	public delegate ResultT FuncContinuation<out ResultT>(Promise promise);
	public delegate ResultT FuncContinuation<PromiseT, out ResultT>(Promise<PromiseT> promise);
}
