﻿using UnityEngine;
using System.Collections;

public class RouteController
{
    public delegate void OnMessage(string message);
    public delegate void OnEnterPoint(RoutePoint pt);
    public delegate void OnExitPoint(RoutePoint pt);

    public RouteController()
    {
        on_message_ = msg => { };
        on_enter_point_ = pt => { };
        on_exit_point_ = pt => { };
    }
    /// <summary>
    /// 设置路点配置
    /// </summary>
    /// <param name="config"></param>
    public void SetRouteConfig(RouteConfig config)
    {
        Clear();

        route_config_ = config;
        route_config_.SetCurrent(0);
    }
    /// <summary>
    /// 是否旋转
    /// </summary>
    /// <param name="rotate"></param>
    public void SetRotateEnable(bool rotate)
    {
        rotate_ = rotate;
    }
    /// <summary>
    /// 重置
    /// </summary>
    public void Reset()
    {
        move_length_ = 0;
        route_config_.SetCurrent(0);
    }
    /// <summary>
    /// 重置移动
    /// </summary>
    public void ResetMove()
    {
        move_length_ = 0;
    }
    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        if(route_config_)
        {
            route_config_.gameObject.SetActive(false);
            GameObject.Destroy(route_config_.gameObject);
			route_config_ = null;
        }
        move_length_ = 0;
    }
    /// <summary>
    /// 销毁数据
    /// </summary>
    public void Destroy()
    {
        Clear();
    }

    public void Update(float dt)
    {
        if(is_finish)
            return;

        //等待时间
        wait_time_ += dt;
        if(wait_time_ <= current_point.keeptime)
            return;
        dt = wait_time_ - current_point.keeptime;
        wait_time_ = current_point.keeptime;

        float velocity = GetCurrenVelocity();
        
        move_length_ += velocity * dt;
        while(move_length_ > current_length)
        {
            move_length_ -= current_length;
            on_exit_point_(current_point);
            //是否完成
            if(is_finish)
                break;
            //进入下一路点
            route_config_.SetCurrent(current + 1);

            on_enter_point_(current_point);
            if(!string.IsNullOrEmpty(current_point.message))
                on_message_(current_point.message);

            //计算等待时间
            wait_time_ = move_length_ / velocity;
            if(wait_time_ > current_point.keeptime)
            {
                velocity = GetCurrenVelocity();
                move_length_ = (wait_time_ - current_point.keeptime) * velocity;
            }
            else
            {
                move_length_ = 0;
                break;
            }
        }
    }

    public void UpdateNegative(float dt)
    {
        if(current == 0)
            return;

        wait_time_ += dt;
        if(next_point)
        {
            if(wait_time_ <= next_point.keeptime)
                return;
            dt = wait_time_ - next_point.keeptime;
            wait_time_ = next_point.keeptime;
        }
        else
        {
            dt = wait_time_;
            wait_time_ = 0;
        }

        float velocity = GetCurrenVelocity();
        move_length_ -= velocity * dt;
        while(move_length_ < 0)
        {
            on_exit_point_(current_point);
            //已经到达头部
            if(current == 0)
            {
                move_length_ = 0;
                break;
            }
            //进入下一路点
            route_config_.SetCurrent(current - 1);

            on_enter_point_(current_point);
            if(!string.IsNullOrEmpty(current_point.message))
                on_message_(current_point.message);

            //计算等待时间
            wait_time_ = move_length_ / velocity;
            float keeptime = 0;
            if(next_point != null)
                keeptime = next_point.keeptime;

            if(wait_time_ > keeptime)
            {
                velocity = GetCurrenVelocity();
                move_length_ = route_config.current_length - (wait_time_ - keeptime) * velocity;
            }
            else
            {
                move_length_ = route_config.current_length;
                break;
            }
        }
    }

    public void Update(float dt, GameObject obj)
    {
        Update(dt);
        Apply(obj);
    }

    public void Step(float delta_length)
    {
        float velocity = GetCurrenVelocity();

        move_length_ += delta_length;
        while(move_length_ > current_length)
        {
            move_length_ -= current_length;
            on_enter_point_(current_point);
            //是否完成
            if(is_finish)
                break;
            //进入下一路点
            route_config_.SetCurrent(current + 1);

            on_enter_point_(current_point);
            if(!string.IsNullOrEmpty(current_point.message))
                on_message_(current_point.message);

            //计算等待时间
            if(velocity <= 0.0f)
            {
                move_length_ = 0;
            }
            else
            {
                wait_time_ = move_length_ / velocity;
                if(wait_time_ > current_point.keeptime)
                {
                    velocity = GetCurrenVelocity();
                    move_length_ = (wait_time_ - current_point.keeptime) * velocity;
                }
                else
                {
                    move_length_ = 0;
                    break;
                }
            }
        }
    }

    public void Next()
    {
        on_exit_point_(current_point);
        if(!is_finish)
        {
            route_config_.SetCurrent(current + 1);
            wait_time_ = 0;
            move_length_ = 0;

            on_enter_point_(current_point);
            if(!string.IsNullOrEmpty(current_point.message))
                on_message_(current_point.message);
        }
        else
            move_length_ = current_length;
    }

    public void Next(GameObject obj)
    {
        Next();
        Apply(obj);
    }

    public void Apply(GameObject obj)
    {
        if(!obj)
            return;

        obj.transform.position = GetPoint();
        if(rotate_)
            obj.transform.eulerAngles = new Vector3(0, RouteMath.GetPolarEular(GetTangent()), 0);
    }

    public RoutePoint FindMessage(int start_index, string message)
    {
        if(route_config_ == null || start_index >= route_config_.count)
            return null;
        for(int i = start_index; i < route_config_.count; ++i)
        {
            var p = route_config_[i];
            if(p.message.CompareTo(message) == 0)
                return p;
        }
        return null;
    }

    public RoutePoint FindMessage(string message)
    {
        return FindMessage(0, message);
    }

    public RoutePoint FindMessageFromCurrent(string message)
    {
        return FindMessage(current, message);
    }

    /// <summary>
    /// 获得当前速度
    /// </summary>
    /// <returns></returns>
    public float GetCurrenVelocity()
    {
        float vel = current_point.velocity;
        if(vel <= 0.0f)
            vel = route_config_.velocity;
        return vel;
    }

    public void SetCurrent(int cur)
    {
        route_config_.SetCurrent(cur);
    }
    public Vector3 GetPoint()
    {
        //var t = move_length_ / current_length;
        //return GetPoint(Mathf.Clamp(t, 0, 1));
        return GetPoint(move_length_);
    }
    public Vector3 GetTangent()
    {
        //var t = move_length_ / current_length;
        //return GetTangent(Mathf.Clamp(t, 0, 1));
        return GetTangent(move_length_);
    }
    public Vector3 GetPoint(float l)
    {
        return route_config_.GetPointByLength(l);
    }
    public Vector3 GetTangent(float l)
    {
        return route_config_.GetTangentByLength(l);
    }
    
    /// <summary>
    /// 消息回调
    /// </summary>
    public OnMessage on_message { set { on_message_ = value; } }
    OnMessage on_message_;
    /// <summary>
    /// 进入路点回调
    /// </summary>
    public OnEnterPoint on_enter_point { set { on_enter_point_ = value; } }
    OnEnterPoint on_enter_point_;
    /// <summary>
    /// 离开路点回调
    /// </summary>
    public OnExitPoint on_exit_point { set { on_exit_point_ = value; } }
    OnExitPoint on_exit_point_;
    /// <summary>
    /// 路点配置
    /// </summary>
    public RouteConfig route_config { get { return route_config_; } }
    RouteConfig route_config_;
    /// <summary>
    /// 数量
    /// </summary>
    public int count { get { return route_config_.count; } }
    /// <summary>
    /// 返回指针
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public RoutePoint this[int index]
    {
        get
        {
            if(route_config_ == null)
                return null;
            return route_config_[index];
        }
    }
    
    /// <summary>
    /// 当前序号
    /// </summary>
    public int current { get { return route_config_.current; } }
    /// <summary>
    /// 当前路点
    /// </summary>
    public RoutePoint current_point { get { return route_config_.current_point; } }
    /// <summary>
    /// 加一个点
    /// </summary>
    public RoutePoint next_point { get { return route_config_.next_point; } }
    /// <summary>
    /// 当前长度
    /// </summary>
    public float current_length { get { return route_config_.current_length; } }
    /// <summary>
    /// 是否完成
    /// </summary>
    public bool is_finish 
    { 
        get 
        {
            return !route_config_ || current + 1 >= count; 
        }
    }
    /// <summary>
    /// 当前进度
    /// </summary>
    public float t { get { return move_length_ / current_length; } }
    /// <summary>
    /// 当前节点下，移动长度
    /// </summary>
    public float move_length { get { return move_length_; } }
    float move_length_ = 0;
    /// <summary>
    /// 等待时间
    /// </summary>
    float wait_time_ = 0;
    /// <summary>
    /// 是否旋转
    /// </summary>
    bool rotate_ = false;
}
