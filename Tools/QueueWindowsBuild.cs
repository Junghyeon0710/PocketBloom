// unity command eval_file용: Inspector 갱신에 의존하는 delayCall 대신 Editor update에서 실행.
// 이 작업이 이전에 등록한 BuildWindows 콜백만 확인 후 제거한다.
int removed = 0;
var pending = UnityEditor.EditorApplication.delayCall;
if (pending != null)
foreach (var callback in pending.GetInvocationList())
{
    if (!(callback.Method.DeclaringType.FullName ?? "").StartsWith("PipelineEvaluation.PipelineEval_")) continue;
    var body = callback.Method.GetMethodBody()?.GetILAsByteArray();
    if (body == null) continue;
    for (int index = 0; index + 4 < body.Length; index++)
    {
        if (body[index] != 0x28) continue;
        System.Reflection.MethodBase target;
        try { target = callback.Method.Module.ResolveMethod(System.BitConverter.ToInt32(body, index + 1)); }
        catch { continue; }
        if (target.DeclaringType != typeof(PocketBloom.Editor.BloomProjectBuilder) || target.Name != "BuildWindows") continue;
        UnityEditor.EditorApplication.delayCall -= (UnityEditor.EditorApplication.CallbackFunction)callback;
        removed++; break;
    }
}
UnityEditor.EditorApplication.CallbackFunction build = null;
build = () =>
{
    if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating || UnityEditor.BuildPipeline.isBuildingPlayer) return;
    UnityEditor.EditorApplication.update -= build;
    PocketBloom.Editor.BloomProjectBuilder.BuildWindows();
};
UnityEditor.EditorApplication.update += build;
return new { removedStaleBuildCallbacks = removed, scheduled = true };
