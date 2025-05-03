# GraduationWork
In this project, I am developing an alternative method for line-of-sight calculations, utilizing depth maps instead of raycasting.

To achieve this, agents have a camera that renders depth maps of the environment and the vision targets. Then I compare the two textures pixel-by-pixel to determine if there is any point where the target is not blocked by the environment, and therefore is detectable by the agent.
After I am done with the implementation, I will analyse the performance of this method.

## Research Question

**RQ1:** How do ray casting and depth map comparison methods compare for vision detection in games in terms of performance and development efficiency?

**RQ2:** For which agent count and target count parameters, if any, does depth map comparison method result in better performance than ray casting?
