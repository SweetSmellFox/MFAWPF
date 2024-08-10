namespace MFAWPF.Utils;

public static class TaskExtensions
{
    public static Dictionary<TKey, TaskModel> MergeTaskModels<TKey>(
        this IEnumerable<KeyValuePair<TKey, TaskModel>> taskModels,
        IEnumerable<KeyValuePair<TKey, TaskModel>> additionalModels)
    {
        if (additionalModels == null)
            return taskModels.ToDictionary();
        return taskModels
            .Concat(additionalModels)
            .GroupBy(pair => pair.Key)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var mergedModel = group.First().Value;
                    foreach (var taskModel in group.Skip(1))
                    {
                        mergedModel.Merge(taskModel.Value);
                    }

                    return mergedModel;
                }
            );
    }
}