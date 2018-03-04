using System;
using System.Collections.Generic;

public class Dijkstra<T>
{
    /// <summary>
    /// ���
    /// </summary>
    public class Result
    {
        public List<T> GetRoad(T end)
        {
            if(!road.ContainsKey(end))
                return null;

            List<T> list = new List<T>();
            list.Add(end);

            T prev = default(T);
            T next = end;
            while(!(prev = road[next]).Equals(next))
            {
                list.Add(prev);
                next = prev;
            }

            return list;
        }

        /// <summary>
        /// ·�����ӵ�
        /// </summary>
        public Dictionary<T, T> road;
        /// <summary>
        /// �ܾ���
        /// </summary>
        public Dictionary<T, float> dist;
    }

    /// <summary>
    /// ��ӵ�
    /// </summary>
    public void Add(T point)
    {
        points_.Add(point);
        graph_[point] = new Dictionary<T, float>();
    }
    /// <summary>
    /// ��ӵ㼯��
    /// </summary>
    public void Add(IEnumerable<T> points)
    {
        foreach(var p in points)
            Add(p);
    }

    public void Clear()
    {
        points_.Clear();
        graph_.Clear();
    }

    /// <summary>
    /// ���õ����֮�����
    /// </summary>
    /// <param name="start_point">���</param>
    /// <param name="end_point">�յ�</param>
    /// <param name="dist">����</param>
    /// <param name="oneway">�Ƿ�Ϊ����</param>
    public void SetDist(T start_point, T end_point, float dist, bool oneway)
    {
        if(!points_.Contains(start_point))
            Add(start_point);
        graph_[start_point][end_point] = dist;
        if(!oneway)
            SetDist(end_point, start_point, dist, true);
    }

    /// <summary>
    /// ��Ѱ��̾����
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="dist"></param>
    /// <returns></returns>
    static T FindMinPoint(HashSet<T> queue, Dictionary<T, float> dist)
    {
        var min_dist = float.MaxValue;
        T point = default(T);
        foreach(var p in queue)
        {
            var d = dist[p];
            if(d < min_dist)
            {
                min_dist = d;
                point = p;
            }
        }
        return point;
    }
    /// <summary>
    /// ��õ����ľ��룬��ֵ��ʾ������
    /// </summary>
    /// <param name="start_point"></param>
    /// <param name="end_point"></param>
    /// <returns></returns>
    float GetDist(T start_point, T end_point)
    {
        var dists = graph_[start_point];
        float dist = 0;
        if(dists.TryGetValue(end_point, out dist))
            return dist;
        return -1;
    }
    /// <summary>
    /// ����·����
    /// </summary>
    /// <param name="start_point"></param>
    /// <returns></returns>
    public Result Emit(T start_point)
    { 
        Result result = new Result();
        if(!points_.Contains(start_point))
            return result;

        Dictionary<T, T> road = new Dictionary<T, T>();
        Dictionary<T, float> dist = new Dictionary<T, float>();
        HashSet<T> queue = new HashSet<T>();
        foreach(var p in points_)
        {
            dist[p] = float.MaxValue;
            queue.Add(p);
        }
        dist[start_point] = 0;
        road[start_point] = start_point;

        while(queue.Count > 0)
        {
            var p = FindMinPoint(queue, dist);
            if(p == null)
                //ʣ�¹�����
                break;

            //�Ƴ���
            queue.Remove(p);
            foreach(var pp in points_)
            {
                var d = GetDist(p, pp);
                if(d > 0 && dist[pp] > dist[p] + d)
                {
                    dist[pp] = dist[p] + d;
                    road[pp] = p;
                }
            }
        }

        result.road = road;
        result.dist = dist;
        return result;
    }

    /// <summary>
    /// �㼯
    /// </summary>
    HashSet<T> points_ = new HashSet<T>();
    /// <summary>
    /// �����֮�������ձ�(����)
    /// </summary>
    Dictionary<T, Dictionary<T, float>> graph_ = 
        new Dictionary<T,Dictionary<T,float>>();
}
