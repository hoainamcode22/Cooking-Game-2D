# CV & Portfolio Update - Complete Package

## 📦 What's Included

### 1. **NGUYEN_HOAI_NAM_CV_FRESHERS.html**
✅ **Status:** Ready to use
- Text-only format (white text, no highlights) - matching Ha Gia Dat's CV style
- Removed "Core Gameplay Systems & Performance" subtitle
- Added comprehensive WebGL optimization case study
- Bổ sung chi tiết: Cooking-Game-2D (Farm) & SkyBound projects
- Professional HTML format - can be printed to PDF or opened in browser

**How to use:**
```
Option A: Open in browser → Right-click → Print to PDF
Option B: Convert using online tool (HtmlToPDF.cc)
Option C: Keep as HTML for sharing via email/online
```

---

### 2. **CASE_STUDY_WebGL_Optimization.md**
✅ **Status:** Ready for portfolio website
- Professional case study document
- Suitable for LinkedIn, personal portfolio, or blog
- Includes:
  - Executive summary with metrics
  - Root cause analysis with diagrams
  - Technical solutions with code examples
  - Performance profiling results
  - Key learnings & recommendations

**How to use:**
```
1. Paste into portfolio website (convert Markdown to HTML)
2. Include link in CV/LinkedIn under "Featured Work"
3. Reference in interviews when discussing optimization
4. Publish on dev.to or Medium as technical article
```

---

### 3. **PROJECT_DETAILS_FOR_PORTFOLIO.md**
✅ **Status:** Reference document - use for interviews & talking points
- Complete system documentation for both projects
- Code examples and architecture decisions
- Performance profiling data from real devices
- Interview preparation tips
- What to highlight for "Fresher" positions

**How to use:**
```
1. Before job interviews - review the "Interview Talking Points" section
2. For technical assessments - reference similar optimization patterns
3. Portfolio website - extract sections for detailed project descriptions
4. Practice pitch: 2-minute summary of iOS crash fix (see key learnings)
```

---

## 🎯 Recommended Next Steps

### For Job Applications (Fresher Positions):

**Step 1: Create PDF from HTML CV**
```bash
# Option A: Browser print
1. Open NGUYEN_HOAI_NAM_CV_FRESHERS.html in Chrome
2. Ctrl+P (or Cmd+P on Mac) → Print to PDF
3. Save as "NGUYEN_HOAI_NAM_CV.pdf"

# Option B: Online converter
1. Visit https://html2pdf.com/
2. Upload NGUYEN_HOAI_NAM_CV_FRESHERS.html
3. Download PDF
```

**Step 2: Update LinkedIn & Itch.io Bio**
```
📝 Current: "Unity Game Developer · Core Gameplay Systems & Performance"

✅ Suggested: "Unity Game Developer | WebGL Optimization | Gameplay Systems"

Add to description:
"Ranked Top 86 / 1,570 in NYPC 2026 (Master Track). 
Optimized Cooking-Game-2D for cross-platform: 
- Solved iOS crashes (95% → 0%)
- Improved FPS by 3.5x
- Reduced memory by 65-80%"
```

**Step 3: Build Portfolio Website**
Include these sections:
```
📌 Featured Projects
  ├─ Cooking-Game-2D (with CASE_STUDY_WebGL_Optimization.md)
  ├─ SkyBound (with performance metrics)
  └─ NYPC 2026 (Top 86 / 1,570)

📌 Technical Skills
  ├─ Graphics: ASTC Compression, Resolution Scaling, WebGL
  ├─ Performance: Object Pooling, GC Elimination, Profiling
  ├─ Gameplay: FSM, Inventory, Economy Systems, Event-Driven
  └─ Tools: Custom Editor Scripts, ScriptableObjects

📌 About
  └─ Include 2-min pitch from PROJECT_DETAILS_FOR_PORTFOLIO.md
```

---

### Key Talking Points for Interviews:

**Q: "Tell us about your most complex project"**

**Answer Structure (2-3 minutes):**
1. **Problem:** "iOS users couldn't play my game - 95% crash rate"
2. **Analysis:** "Debugged and found WebKit memory limits, VRAM issues with Retina displays"
3. **Solution:** "Built adaptive tier scaler, ASTC compression, linear memory allocation"
4. **Results:** "Went from 0% iOS access to 100% stable, 3.5x faster load, 60 FPS"
5. **Learning:** "Device constraints matter - optimization is a feature, not afterthought"

---

**Q: "How do you approach performance optimization?"**

**Answer:**
1. Profile first (don't guess where bottleneck is)
2. Root cause analysis (understand why, not just what)
3. Implement targeted fix (don't over-engineer)
4. Measure impact (quantify before/after)
5. Iterate if needed (rarely first solution is best)

**Example from your work:**
- Used Unity Profiler → found 50+ VFX instantiations per frame
- Implemented object pooling → reduced GC allocations from 35-40 KB/s to 0 KB/s
- Result: FPS improved from 28 FPS to 60 FPS stable

---

**Q: "What's your experience with WebGL/mobile optimization?"**

**Answer:**
"I've shipped WebGL and Android builds. Key learnings:
- ASTC compression saves 80% VRAM (16MB → 2.7MB per sprite)
- Linear heap allocation safer than geometric for iOS
- Resolution clamping (1080p render, OS upscales) cuts GPU fillrate 50-70%
- Object pooling essential for frame rate consistency

Real example: iPhone crashes from OOM → fixed by:
  1. Reducing VRAM via ASTC
  2. Capping resolution to 1080p
  3. Linear 16MB heap increments instead of exponential
  4. Result: iOS now playable, 60 FPS consistent"

---

## ✅ Checklist Before Applying

- [ ] CV is in text-only format (HTML or PDF)
- [ ] CV is grammatically polished (no typos)
- [ ] GitHub links in CV are working and have recent commits
- [ ] Itch.io links work and show playable games
- [ ] Portfolio website is live (or at least GitHub Pages)
- [ ] Can explain WebGL optimization in 2-3 minutes
- [ ] Can show code examples of object pooling, FSM, or similar
- [ ] Have 2-3 NYPC or other achievement links ready

---

## 📊 Metrics to Reference

### Cooking-Game-2D
- **iOS Crash Fix:** 95% → 0% (complete fix)
- **VRAM Reduction:** 520-680 MB → 130-165 MB (75% improvement)
- **RAM Peak:** 1.5-2.0 GB → 300-450 MB (65-80% improvement)
- **Load Time:** 18.5-26s → 4.2-6.8s (3.5x faster)
- **FPS:** 28-42 FPS → 58-60 FPS (2x improvement)
- **Download Size:** 150 MB → 38-44 MB (72% smaller)

### SkyBound
- **Build Size:** 41 MB (lightweight)
- **Input Latency:** 80ms → <16ms (5x faster)
- **FPS Consistency:** Stable 60.0 ± 0.1 FPS
- **Memory:** <100 MB runtime
- **Supported Devices:** 95%+ of Android, 100% iOS (with DPR scaling)

### NYPC Achievement
- **Ranking:** Top 86 / 1,570 competitors (Top 5%)
- **Badges:** 7 NYPC Competency Badges awarded
- **Bot Performance:** 0 timeout penalties (strict 100ms per-turn SLA)

---

## 🔗 Quick Links to Update

**LinkedIn:**
- Add these projects to "Featured" section
- Update headline to emphasize optimization skills
- Add media (screenshots of Itch.io pages)

**GitHub:**
- Ensure Cooking-Game-2D & SkyBound repos are public
- Add comprehensive README.md with metrics
- Pin repositories to profile

**Itch.io:**
- Ensure games are published and playable
- Add description referencing CV/portfolio
- Update bio with achievements

**Portfolio Website:**
- Add "Projects" section with embedded games
- Add "Case Studies" section with CASE_STUDY_WebGL_Optimization.md
- Add "About" section with 2-min pitch

---

## 📄 File Descriptions

| File | Format | Purpose | Audience |
|------|--------|---------|----------|
| NGUYEN_HOAI_NAM_CV_FRESHERS.html | HTML/PDF | Professional CV | Recruiters, job portals |
| CASE_STUDY_WebGL_Optimization.md | Markdown | Technical deep-dive | Technical interviewers, blog readers |
| PROJECT_DETAILS_FOR_PORTFOLIO.md | Markdown | Project documentation | Self-reference, interview prep |
| README.md | Markdown | This guide | You (getting started) |

---

## 🚀 Expected Outcomes

Using these materials in your job search:

**For Fresher Positions:**
- ✅ Stand out from other candidates (most don't quantify impact)
- ✅ Show ownership (year-long project, shipped to public)
- ✅ Demonstrate problem-solving (debugged platform-specific crashes)
- ✅ Prove CS fundamentals (object pooling, FSM, event-driven)
- ✅ Speak confidently in technical interviews (prepared talking points)

**Typical Next Steps:**
1. Technical screening (coding test or case study)
2. System design interview (discuss architecture decisions)
3. Behavioral interview (use NYPC + project stories)
4. Offer negotiation

---

## 💡 Pro Tips

**In Interviews:**
- Lead with metrics (95% → 0%, 3.5x faster)
- Explain the journey (problem → analysis → solution → results)
- Show you care about users (iOS was broken, you fixed it)
- Mention the learning (not just the achievement)

**In Code Reviews:**
- Reference your optimization experience
- Ask about performance constraints early
- Suggest profiling before premature optimization

**In Team Meetings:**
- Offer to help with performance issues
- Share optimization patterns you've learned
- Build reputation as the performance person

---

## ❓ FAQ

**Q: Should I mention NYPC in every interview?**
A: Yes, but briefly. Say "Top 86 / 1,570 in national competition" once, then pivot to game projects. Interviewers care more about shipped work.

**Q: What if the interviewer asks about graphics/game design?**
A: Redirect to optimization & systems work. "Graphics isn't my focus - I'm more interested in gameplay architecture and performance. In Cooking-Game-2D, I built recipe systems and livestock AI..." This is more impressive for Fresher roles anyway.

**Q: Do I need a fancy portfolio website?**
A: No. A simple one-pager with links to GitHub, Itch.io, and this case study is enough. Focus on content, not design.

**Q: Should I apply to senior positions too?**
A: With 1 year of experience, target Junior/Mid-level. Consider senior roles only if the job post says "Fresher candidates welcome" or doesn't list strict years requirement.

---

## 📞 Next Actions

1. **This Week:**
   - [ ] Convert HTML CV to PDF
   - [ ] Update LinkedIn
   - [ ] Clean up GitHub repos

2. **Next Week:**
   - [ ] Build portfolio website (or use GitHub Pages)
   - [ ] Start applying to 5-10 junior positions
   - [ ] Practice 2-min pitch with a friend

3. **Month 1:**
   - [ ] Iterate based on interview feedback
   - [ ] Update CV with any new projects
   - [ ] Collect job offer feedback for negotiation

---

**Good luck! 🚀 You've built impressive work - now go show it off.**

Questions? Review PROJECT_DETAILS_FOR_PORTFOLIO.md for interview prep or CASE_STUDY_WebGL_Optimization.md for technical reference.
